using MockFundraisingApp.Models;

namespace MockFundraisingApp.ViewModels
{
    public class RequestsListVm
    {
        public List<FundraisingRequest> Requests { get; set; } = new();

        public string Sort { get; set; } = "recent";
        public string? Type { get; set; }

        public string? Cursor { get; set; }
        public string? NextCursor { get; set; }

        public int PageSize { get; set; } = 10;
        public int PageNumber { get; set; } = 1;

    }
}
