using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fall2024_Assignment3_knlim.Data;
using Fall2024_Assignment3_knlim.Models;

using System.ClientModel;
using System.Text.Json.Nodes;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using VaderSharp2;

namespace Fall2024_Assignment3_knlim.Controllers
{
    public class MovieController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public MovieController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: Movie
        public async Task<IActionResult> Index()
        {
            return View(await _context.Movie.ToListAsync());
        }

        // GET: Movie/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }


            var actors = await _context.MovieActor
            .Include(cs => cs.Actor)
            .Where(cs => cs.MovieId == movie.Id)
            .Select(cs => cs.Actor)
            .ToListAsync();

            // AI
            var ApiKey = _config["AzureOpenAi:ApiKey"] ?? throw new Exception("OpenAI: Key does not exist in the current configuration");
            var ApiEndpoint = _config["AzureOpenAi:Endpoint"] ?? throw new Exception("OpenAI: Endpoint does not exist in the current configuraiton");
            var AiDeployment = "gpt-35-turbo";

            ApiKeyCredential ApiCredential = new(ApiKey);

            ChatClient client = new AzureOpenAIClient(new Uri(ApiEndpoint), ApiCredential).GetChatClient(AiDeployment);

            var reviews = new List<string>();
            var MovieTitle = movie.Title;
            var MovieYear = movie.ReleaseYear;


            string[] personas = { "is harsh", "loves romance", "loves comedy", "loves thrillers", "loves fantasy" };

            foreach (string persona in personas)
            {
                var messages = new ChatMessage[]
                {
                    new SystemChatMessage($"You are a film reviewer and film critic who {personas}. Generate 10 responses, each relating to the selected movie. "),
                    new UserChatMessage($"How would you rate the move {MovieTitle} released in {MovieYear} out of 10 in less than 175 words?")
                };
                var chatCompletionOptions = new ChatCompletionOptions
                {
                    MaxOutputTokenCount = 1000,
                };
                ClientResult<ChatCompletion> result = await client.CompleteChatAsync(messages, chatCompletionOptions);

                reviews.Add(result.Value.Content[0].Text);
                //reviews = result.Value.Content[0].Text.Split('|').Select(s => s.Trim()).ToArray();
                //Thread.Sleep(TimeSpan.FromSeconds(10)); 
            }
            var analyzer = new SentimentIntensityAnalyzer();
            double sentimentTotal = 0;
            var reviews_and_sentiment = new List<Object[]>();

            for (int i = 0; i < reviews.Count; i++)
            {
                string review = reviews[i];
                SentimentAnalysisResults sentiment = analyzer.PolarityScores(review);
                sentimentTotal += sentiment.Compound;

                reviews_and_sentiment.Add(new Object[] { review, sentiment.Compound });
            }

            double sentimentAverage = sentimentTotal / reviews.Count;

            var va = new MovieDetailsViewModel(movie, actors, reviews_and_sentiment, sentimentAverage);
            return View(va);
        }

        // GET: Movie/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movie/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,IMDBLink,Genre,ReleaseYear,Photo")] Movie movie, IFormFile? photo)
        {
            if (ModelState.IsValid)
            {
                if (photo != null && photo.Length > 0)
                {
                    using var memoryStream = new MemoryStream(); // Dispose() for garbage collection 
                    await photo.CopyToAsync(memoryStream);
                    movie.Photo = memoryStream.ToArray();
                }
                _context.Add(movie);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movie/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        // POST: Movie/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,IMDBLink,Genre,ReleaseYear,Photo")] Movie movie)
        {
            if (id != movie.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movie/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movie/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movie.FindAsync(id);
            if (movie != null)
            {
                _context.Movie.Remove(movie);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movie.Any(e => e.Id == id);
        }
    }
}
