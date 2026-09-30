using CRM.api.DTOs;
using System.Globalization;
using System.Text;

namespace CRM.api.Services;

public class CsvExportService
{
    // UTF-8 with BOM ensures Microsoft Excel properly renders non-ASCII characters
    private static readonly Encoding Utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    /// <summary>
    /// Exports the rental ledger to a local file.
    /// </summary>
    public async Task ExportRentalLedgerAsync(
        string filePath,
        IEnumerable<RentalLedgerRowDto> records,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 65536,
            useAsync: true);

        await ExportRentalLedgerToStreamAsync(fileStream, records, cancellationToken);
    }

    /// <summary>
    /// Exports the rental ledger to any stream (ideal for ASP.NET Core FileStreamResult or in-memory operations).
    /// </summary>
    public async Task ExportRentalLedgerToStreamAsync(
        Stream destinationStream,
        IEnumerable<RentalLedgerRowDto> records,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destinationStream);
        ArgumentNullException.ThrowIfNull(records);

        // leaveOpen: true allows callers (like ASP.NET controllers) to manage destinationStream lifecycle
        await using var writer = new StreamWriter(destinationStream, Utf8WithBom, bufferSize: 65536, leaveOpen: true);

        // Header Row - Includes derived columns (Duration and Total Billed)
        await writer.WriteLineAsync(
            "Booking Code,Client,Garments,Rental Days,Start Date,End Date,Rental Fee (PHP),Security Deposit (PHP),Total Billed (PHP),Stage,Payment Method".AsMemory(),
            cancellationToken);

        decimal totalRevenue = 0m;
        decimal totalDeposits = 0m;
        int count = 0;

        foreach (var row in records)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int rentalDays = Math.Max(1, (row.RentalEndDate.Date - row.RentalStartDate.Date).Days);
            decimal totalBilled = row.RentalFee + row.SecurityDeposit;

            totalRevenue += row.RentalFee;
            totalDeposits += row.SecurityDeposit;
            count++;

            string line = string.Join(",",
                EscapeCsv(row.BookingCode),
                EscapeCsv(row.ClientName),
                EscapeCsv(row.GarmentSummary),
                rentalDays.ToString(CultureInfo.InvariantCulture),
                row.RentalStartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                row.RentalEndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                row.RentalFee.ToString("F2", CultureInfo.InvariantCulture),
                row.SecurityDeposit.ToString("F2", CultureInfo.InvariantCulture),
                totalBilled.ToString("F2", CultureInfo.InvariantCulture),
                EscapeCsv(row.Stage),
                EscapeCsv(row.PaymentMethod)
            );

            await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
        }

        // Summary / Totals Audit Footer Row
        if (count > 0)
        {
            decimal grandTotal = totalRevenue + totalDeposits;
            string summaryLine = string.Join(",",
                EscapeCsv($"TOTALS ({count} bookings)"),
                "\"\"",
                "\"\"",
                "\"\"",
                "\"\"",
                "\"\"",
                totalRevenue.ToString("F2", CultureInfo.InvariantCulture),
                totalDeposits.ToString("F2", CultureInfo.InvariantCulture),
                grandTotal.ToString("F2", CultureInfo.InvariantCulture),
                "\"\"",
                "\"\""
            );

            await writer.WriteLineAsync(summaryLine.AsMemory(), cancellationToken);
        }

        await writer.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Sanitizes and escapes CSV field values, guarding against formula injection and delimiters.
    /// </summary>
    private static string EscapeCsv(string? field)
    {
        if (string.IsNullOrEmpty(field)) return "\"\"";

        string val = field;

        // Mitigate CSV / DDE formula injection vulnerabilities in spreadsheet applications
        if (val.StartsWith('=') || val.StartsWith('+') || val.StartsWith('-') || val.StartsWith('@') || val.StartsWith('\t'))
        {
            val = "'" + val;
        }

        // Standard RFC 4180 CSV escaping
        if (val.Contains(',') || val.Contains('"') || val.Contains('\n') || val.Contains('\r') || val.StartsWith('\''))
        {
            return $"\"{val.Replace("\"", "\"\"")}\"";
        }

        return val;
    }
}