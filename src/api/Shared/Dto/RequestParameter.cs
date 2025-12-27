namespace Shared;
//public class RequestParameter
//{
//    public int? Page { get; set; } = 1;
//    public int? Length { get; set; } = 10;
//    public Dictionary<string, string> Sorts { get; set; } = [];
//    public List<object[]> Filters { get; set; } = [];
//    public string[] Fields { get; set; } = [];
//    public bool SingleResult { get; set; } = false;

//    public void AddFilter(string key, string opr, string value) => Filters.Add(new[] { key, opr, value });
//    public void AddFilter(string key, string value) => Filters.Add(new[] { key, "=", value });
//    public void AddFilter(string key, string opr, object value) => Filters.Add([key, opr, value]);
//    public void AddFilter(string key, object value) => Filters.Add([key, "=", value]);
//}