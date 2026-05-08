using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Factlens.Core.DTOs
{
    public class FeedbackRequest
    {
        public int SearchRecordId { get; set; }
        public bool Helpful { get; set; }
        public int? Rating { get; set; }
        public bool ReportIncorrect { get; set; }
        public string Comment { get; set; }
    }
}
