using System;
using System.ComponentModel.DataAnnotations;

namespace Fall2024_Assignment3_knlim.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string IMDBLink { get; set; }
        public string Genre { get; set; }
        public int ReleaseYear { get; set; }
        public byte[]? Photo { get; set; }
    }
}

