using Google.Cloud.Firestore;

namespace MockFundraisingApp.Models
{
    [FirestoreData]
    public class FundraisingRequest
    {
        [FirestoreDocumentId] public string Id { get; set; } = "";
        [FirestoreProperty] public string Requester { get; set; } = "";
        [FirestoreProperty] public double DonationLimit { get; set; }
        [FirestoreProperty] public string RequestType { get; set; } = "";
        [FirestoreProperty] public double CurrentAmount { get; set; }
        [FirestoreProperty] public long DonationCount { get; set; }
        [FirestoreProperty] public double ProgressPct { get; set; }
        [FirestoreProperty] public double RemainingAmount { get; set; }
        [FirestoreProperty] public double RemainingPct { get; set; }

        [FirestoreProperty] public Timestamp CreatedAt { get; set; } = Timestamp.GetCurrentTimestamp();
    }
}
