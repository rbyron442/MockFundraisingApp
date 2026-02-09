using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MockFundraisingApp.Models;
using MockFundraisingApp.Services;
using MockFundraisingApp.ViewModels;

namespace MockFundraisingApp.Controllers
{
    public class RequestsController : Controller
    {
        private readonly RequestsStore _store;
        private readonly IWebHostEnvironment _env;

        public RequestsController(RequestsStore store, IWebHostEnvironment env)
        {
            _store = store;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string sort = "recent", string? type = null, string? cursor = null, string? dir = null)
        {
            const string CursorStackKey = "cursorStack";
            const string PageNumberKey = "pageNumber";
            const string PagingKey = "pagingKey";

            // Reset pagination when filters/sort change
            var newKey = $"{sort}|{type}";
            var oldKey = HttpContext.Session.GetString(PagingKey);

            // Load cursor stack from session
            var stackRaw = HttpContext.Session.GetString(CursorStackKey);
            var cursorStack = string.IsNullOrWhiteSpace(stackRaw)
                ? new Stack<string>()
                : new Stack<string>(stackRaw.Split('|', StringSplitOptions.RemoveEmptyEntries));

            var pageNumber = HttpContext.Session.GetInt32(PageNumberKey) ?? 1;

            if (!string.Equals(oldKey, newKey, StringComparison.Ordinal))
            {
                cursorStack.Clear();
                pageNumber = 1;
                HttpContext.Session.SetString(PagingKey, newKey);
            }

            // Update cursor stack + page number based on navigation direction
            if (string.Equals(dir, "next", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(cursor))
                {
                    cursorStack.Push(cursor);
                    pageNumber++;
                }
            }
            else if (string.Equals(dir, "prev", StringComparison.OrdinalIgnoreCase))
            {
                if (cursorStack.Count > 0)
                {
                    cursorStack.Pop();
                    pageNumber = Math.Max(1, pageNumber - 1);
                }
                else
                {
                    pageNumber = 1;
                }
            }
            else
            {
                cursorStack.Clear();
                pageNumber = 1;
            }

            // Persist updated state
            HttpContext.Session.SetString(CursorStackKey, string.Join("|", cursorStack.Reverse()));
            HttpContext.Session.SetInt32(PageNumberKey, pageNumber);

            var currentCursor = cursorStack.Count > 0 ? cursorStack.Peek() : null;

            var (items, nextCursor) = await _store.GetPageAsync(10, sort, type, currentCursor);

            var vm = new RequestsListVm
            {
                Requests = items,
                Sort = sort,
                Type = type,
                Cursor = currentCursor,
                NextCursor = nextCursor,
                PageSize = 10,
                PageNumber = pageNumber
            };

            ViewBag.Types = new[] { "Medical", "Community", "Education", "Emergency", "Other" };
            ViewBag.HasPrev = cursorStack.Count > 0;

            return View(vm);
        }

        [HttpGet("Requests/Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Missing request id.");

            var req = await _store.GetAsync(id);
            if (req == null) return NotFound();

            var donations = await _store.GetDonationsAsync(id, 50);

            var vm = new RequestDetailsVm
            {
                Request = req,
                Donations = donations
            };

            return View(vm);
        }

        [HttpPost("Requests/Details/{id}/donate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Donate(string id, RequestDetailsVm vm)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Missing request id.");

            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Details), new { id });

            if (vm.Amount <= 0)
            {
                TempData["Err"] = "Donation amount must be greater than 0.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var donor = vm.DonorName.Trim();
            await _store.AddDonationAsync(id, donor, vm.Amount);

            TempData["Ok"] = "Thank you for your donation!";
            return RedirectToAction(nameof(Details), new { id });
        }


        [Authorize]
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Types = new[] { "Medical", "Community", "Education", "Emergency", "Other" };
            return View(new CreateRequestVm());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRequestVm vm)
        {
            ViewBag.Types = new[] { "Medical", "Community", "Education", "Emergency", "Other" };

            if (!ModelState.IsValid) return View(vm);

            var req = new FundraisingRequest
            {
                Requester = vm.Requester.Trim(),
                DonationLimit = vm.DonationLimit,
                RequestType = vm.RequestType
            };

            try
            {
                var id = await _store.CreateAsync(req);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (InvalidOperationException ex)
            {
                // Duplicate requester
                ModelState.AddModelError(nameof(vm.Requester), ex.Message);
                return View(vm);
            }
        }
    }
}