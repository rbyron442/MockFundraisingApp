using Google.Cloud.Firestore;

namespace MockFundraisingApp.Models
{
    [FirestoreData]
    public class Donation
    {
        [FirestoreDocumentId] public string Id { get; set; } = "";
        [FirestoreProperty] public string DonorName { get; set; } = "";
        [FirestoreProperty] public double Amount { get; set; }
        [FirestoreProperty] public Timestamp CreatedAt { get; set; } = Timestamp.GetCurrentTimestamp();
    }
}
