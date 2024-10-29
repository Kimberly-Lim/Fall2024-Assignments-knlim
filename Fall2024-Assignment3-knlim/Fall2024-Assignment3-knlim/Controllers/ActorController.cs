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
    public class ActorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public ActorController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<IActionResult> GetActorPhoto(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor.FindAsync(id);
            if (actor == null || actor.Photo == null)
            {
                return NotFound();
            }

            var data = actor.Photo;
            return File(data, "image/jpg");
        }

        // GET: Actor
        public async Task<IActionResult> Index()
        {
            return View(await _context.Actor.ToListAsync());
        }

        // GET: Actor/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor
                .FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null)
            {
                return NotFound();
            }

            var movies = await _context.MovieActor
            .Include(cs => cs.Movie)
            .Where(cs => cs.ActorId == actor.Id)
            .Select(cs => cs.Movie)
            .ToListAsync();

            // AI


            var ApiKey = _config["AzureOpenAi:ApiKey"] ?? throw new Exception("OpenAI: Key does not exist in the current configuration");
            var ApiEndpoint = _config["AzureOpenAi:Endpoint"] ?? throw new Exception("OpenAI: Endpoint does not exist in the current configuraiton");
            var AiDeployment = "gpt-35-turbo";

            ApiKeyCredential ApiCredential = new(ApiKey);

            ChatClient client = new AzureOpenAIClient(new Uri(ApiEndpoint), ApiCredential).GetChatClient(AiDeployment);

            var ActorName = actor.Name;

            var messages = new ChatMessage[]
            {
               new SystemChatMessage("You reprsent the Twitter social media platform. One reponse should contain the username and tweet. Generate 21 responses, each relating to the selected actor"),
               new UserChatMessage($"Generate users and their tweets about the actor {ActorName}.")
            };

            var ChatCompletions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 1000,
            };

            ClientResult<ChatCompletion> result = await client.CompleteChatAsync(messages);
            string[] twitter = result.Value.Content[0].Text.Split('|').Select(s => s.Trim()).ToArray();

            var analyzer = new SentimentIntensityAnalyzer();
            double sentimentTotal = 0;
            var tweets_and_sentiments = new List<object[]>();

            for (int i = 0; i < twitter.Length; i++)
            {
                string review = twitter[i];
                SentimentAnalysisResults sentiment = analyzer.PolarityScores(review);
                sentimentTotal += sentiment.Compound;

                tweets_and_sentiments.Add(new Object[] { review, sentiment.Compound });
            }

            double sentimentAverage = sentimentTotal / twitter.Length;

            var va = new ActorDetailsViewModel(actor, movies, tweets_and_sentiments, sentimentAverage); 
            return View(va);
        }

        // GET: Actor/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Actor/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Gender,IMDBLink,Photo")] Actor actor, IFormFile? photo)
        {
            if (ModelState.IsValid)
            {
                if (photo != null && photo.Length > 0)
                {
                    using var memoryStream = new MemoryStream(); // Dispose() for garbage collection 
                    await photo.CopyToAsync(memoryStream);
                    actor.Photo = memoryStream.ToArray();
                }
                
                _context.Add(actor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(actor);
        }

        // GET: Actor/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor.FindAsync(id);
            if (actor == null)
            {
                return NotFound();
            }
            return View(actor);
        }

        // POST: Actor/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Gender,IMDBLink,Photo")] Actor actor)
        {
            if (id != actor.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(actor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActorExists(actor.Id))
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
            return View(actor);
        }

        // GET: Actor/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var actor = await _context.Actor
                .FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null)
            {
                return NotFound();
            }

            return View(actor);
        }

        // POST: Actor/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var actor = await _context.Actor.FindAsync(id);
            if (actor != null)
            {
                _context.Actor.Remove(actor);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ActorExists(int id)
        {
            return _context.Actor.Any(e => e.Id == id);
        }
    }
}
