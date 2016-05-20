using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BackgroundTask.Model
{
    public sealed class SourceDefinition
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public string Icon_Light { get; set; }
        public string Icon_Dark { get; set; }
        public string Url { get; set; }
        public string Handler_ID { get; set; }
    }

    public sealed class Source
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public string Icon_Light { get; set; }
        public string Icon_Dark { get; set; }
        public string Url { get; set; }
        public string Handler_ID { get; set; }
        public IList<Article> Articles { get; set; }
    }

    public sealed class Article
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Image { get; set; }
        public string Author { get; set; }
        public string Date { get; set; }
        public string Url { get; set; }
        public string Discussion_Url { get; set; }
        public string Content { get; set; }
    }
}