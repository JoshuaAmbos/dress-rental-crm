using CRM.winforms.Models;
using System.Text;

namespace CRM.winforms.Services;

public class CsvExportService
{
    public async Task ExportRentalLedgerAsync(string filePath, IEnumerable<RentalLedgerRowDto> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        var sb = new StringBuilder();
        sb.AppendLine("Booking Code,Client,Garments,Start Date,End Date,Rental Fee,Security Deposit,Stage,Payment Method");

        foreach (var row in records)
        {
            string client = EscapeCsv(row.ClientName);
            string garments = EscapeCsv(row.GarmentSummary);
            string stage = EscapeCsv(row.Stage);
            string payment = EscapeCsv(row.PaymentMethod);

            sb.AppendLine($"{row.BookingCode},{client},{garments},{row.RentalStartDate:yyyy-MM-dd},{row.RentalEndDate:yyyy-MM-dd},{row.RentalFee:F2},{row.SecurityDeposit:F2},{stage},{payment}");
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
    }

    private static string EscapeCsv(string? field)
    {
        if (string.IsNullOrEmpty(field)) return string.Empty;

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}