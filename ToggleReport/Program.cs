using System.Net.Http.Headers;
using System.Text;
using RestEase;
using Simple.Sqlite;
using Simple.Sqlite.Extension;

var path = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\TogglDesktop\toggldesktop.db");
var connection = ConnectionFactory.CreateConnection(path);
var session = connection.GetAll<Session>("sessions").First();
var togglApi = RestClient.For<ITogglApi>("https://api.track.toggl.com/api/v8");
togglApi.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{session.api_token}:api_token")));
var entries = await togglApi.GetTimeEntries(DateTime.Now.AddDays(-5), DateTime.Now);

var entriesByDate = entries.GroupBy(entry => entry.Start.Date);
var sb = new StringBuilder();
foreach (var dateEntries in entriesByDate.OrderByDescending(grouping => grouping.Key))
{
    if (dateEntries.Key == DateTime.Today)
    {
        sb.AppendLine("Täna:");
        foreach (var entry in dateEntries.DistinctBy(e => e.Description).OrderByDescending(e => e.Start))
        {
            sb.AppendLine($"* {entry.Description}");
        }
    }
    else if (dateEntries.Key == DateTime.Today.AddDays(-1))
    {
        sb.AppendLine("Eile:");
        foreach (var entry in dateEntries.DistinctBy(e => e.Description).OrderByDescending(e => e.Start))
        {
            sb.AppendLine($"* {entry.Description}");
        }
    }
    else
    {
        sb.AppendLine(dateEntries.Key.DayOfWeek switch
        {
            DayOfWeek.Monday => "Esmaspäeval:",
            DayOfWeek.Tuesday => "Teisipäeval:",
            DayOfWeek.Wednesday => "Kolmapäeval:",
            DayOfWeek.Thursday => "Neljapäeval:",
            DayOfWeek.Friday => "Reedel:",
            DayOfWeek.Saturday => "Laupäeval:",
            DayOfWeek.Sunday => "Pühapäeval:",
        });
        foreach (var entry in dateEntries.DistinctBy(e => e.Description).OrderByDescending(e => e.Start))
        {
            sb.AppendLine($"* {entry.Description}");
        }
    }
}
Console.WriteLine(sb.ToString());
Console.ReadLine();

public class Session
{
    public string api_token { get; set; }
}

public class TogglTimeEntry
{
    public DateTime Start { get; set; }
    public string Description { get; set; }
}

public interface ITogglApi
{
    [Header("Authorization")]
    AuthenticationHeaderValue Authorization { get; set; }

    [Get("/time_entries")]
    Task<IList<TogglTimeEntry>> GetTimeEntries([Query("start_date", Format = "yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz")] DateTime startDate, [Query("end_date", Format = "yyyy-MM-ddTHH\\:mm\\:ss.fffffffzzz")] DateTime endDate);
}