using System;
namespace Fall2024_Assignment3_knlim.Models
{
	public class MovieDetailsViewModel
	{
        public Movie Movie { get; set; }
        public IEnumerable<Actor> Actors { get; set; }
        public List<Object[]> Reviews { get; set; }
        public double AvgSentiment { get; set; }

        public MovieDetailsViewModel(Movie movie, IEnumerable<Actor> actors, List<object[]> reviews, double avg)
        {
            Movie = movie;
            Actors = actors;
            Reviews = reviews;
            AvgSentiment = avg;
        }
    }
}

