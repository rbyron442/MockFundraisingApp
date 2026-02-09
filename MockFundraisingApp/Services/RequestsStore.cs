using Google.Cloud.Firestore;
using MockFundraisingApp.Models;

namespace MockFundraisingApp.Services
{
    public class RequestsStore
    {
        private readonly FirestoreDb _db;
        public RequestsStore(FirestoreDb db) => _db = db;

        private CollectionReference Requests => _db.Collection("requests");

        public async Task<string> CreateAsync(FundraisingRequest req)
        {
            // Normalize name for comparison
            var requester = req.Requester.Trim();

            // Query for existing requester
            var existing = await Requests
                .WhereEqualTo("Requester", requester)
                .Limit(1)
                .GetSnapshotAsync();

            if (existing.Count > 0)
                throw new InvalidOperationException("A request for this requester already exists.");

            // Initialize fields
            req.Requester = requester;
            req.CurrentAmount = 0;
            req.RemainingAmount = req.DonationLimit;
            req.ProgressPct = 0;
            req.RemainingPct = 100;

            var doc = await Requests.AddAsync(req);
            return doc.Id;
        }

        public async Task<FundraisingRequest?> GetAsync(string id)
        {
            var snap = await Requests.Document(id).GetSnapshotAsync();
            if (!snap.Exists) return null;

            var req = snap.ConvertTo<FundraisingRequest>();
            req.Id = snap.Id;
            return req;
        }

        public async Task<(List<FundraisingRequest> Items, string? NextCursorId)> GetPageAsync(
            int pageSize,
            string sort,
            string? typeFilter,
            string? cursorId)
        {
            Query q = Requests;

            if (!string.IsNullOrWhiteSpace(typeFilter))
                q = q.WhereEqualTo("RequestType", typeFilter);

            q = sort switch
            {
                "recent" => q.OrderByDescending("CreatedAt"),
                "funded" => q.OrderByDescending("CurrentAmount"),
                "close" => q.OrderBy("RemainingPct"),
                _ => q.OrderByDescending("CreatedAt")
            };

            if (!string.IsNullOrWhiteSpace(cursorId))
            {
                var cursorSnap = await Requests.Document(cursorId).GetSnapshotAsync();
                if (cursorSnap.Exists) q = q.StartAfter(cursorSnap);
            }

            var snap = await q.Limit(pageSize).GetSnapshotAsync();

            var items = snap.Documents.Select(d =>
            {
                var r = d.ConvertTo<FundraisingRequest>();
                r.Id = d.Id;
                return r;
            }).ToList();

            var nextCursor = snap.Count == pageSize ? snap.Documents.Last().Id : null;
            return (items, nextCursor);
        }

        public async Task<List<Donation>> GetDonationsAsync(string requestId, int take = 50)
        {
            var donationsRef = Requests.Document(requestId).Collection("donations");

            var snap = await donationsRef
                .OrderByDescending("CreatedAt")
                .Limit(take)
                .GetSnapshotAsync();

            return snap.Documents.Select(d =>
            {
                var donation = d.ConvertTo<Donation>();
                donation.Id = d.Id;
                return donation;
            }).ToList();
        }

        public async Task AddDonationAsync(string requestId, string donorName, double amount)
        {
            var reqRef = Requests.Document(requestId);
            var donationRef = reqRef.Collection("donations").Document();

            await _db.RunTransactionAsync(async tx =>
            {
                var reqSnap = await tx.GetSnapshotAsync(reqRef);
                if (!reqSnap.Exists) throw new InvalidOperationException("Request not found.");

                var req = reqSnap.ConvertTo<FundraisingRequest>();

                var current = req.CurrentAmount;
                var limit = req.DonationLimit;

                // Cap donation so we never exceed the limit
                var maxAllowed = Math.Max(0, limit - current);
                var applied = Math.Min(amount, maxAllowed);

                var newTotal = current + applied;
                var remaining = Math.Max(0, limit - newTotal);
                var progressPct = limit <= 0 ? 0 : (newTotal / limit) * 100.0;
                if (progressPct > 100) progressPct = 100;

                //To sort closest to completion by remaining percentage
                var remainingPct = limit <= 0 ? 0 : (remaining / limit) * 100.0;
                if (remainingPct < 0) remainingPct = 0;
                if (remainingPct > 100) remainingPct = 100;


                // Create donation record
                tx.Create(donationRef, new Donation
                {
                    DonorName = donorName,
                    Amount = applied,
                    CreatedAt = Timestamp.GetCurrentTimestamp()
                });

                // Update request aggregates
                tx.Update(reqRef, new Dictionary<string, object>
                {
                    ["CurrentAmount"] = newTotal,
                    ["RemainingAmount"] = remaining,
                    ["ProgressPct"] = progressPct,
                    ["RemainingPct"] = remainingPct,
                    ["DonationCount"] = req.DonationCount + 1
                });
            });
        }


    }
}
