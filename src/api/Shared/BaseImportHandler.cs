using Microsoft.Extensions.DependencyInjection;
using System.Data;
using System.Reflection;
using ClosedXML.Excel;
using Microsoft.Extensions.Caching.Memory;
using System.Collections;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using DocumentFormat.OpenXml.Spreadsheet;
using Shared;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared;

public interface IImportable { }
public interface IImportHandler { }

[AttributeUsage(AttributeTargets.Class)]
public class ImportHandlerAttribute : Attribute
{

}

[AttributeUsage(AttributeTargets.Class)]
public class ImportableAttribute : Attribute
{
    public string SheetName { get; set; }
    public string FileName { get; set; }
}


[AttributeUsage(AttributeTargets.Property)]
public class TimeFormat : Attribute
{
}


public abstract class BaseImportHandler : IImportHandler
{
    protected ModuleDbContext AppDb { get; set; }
    public XLWorkbook Workbook { get; private set; }

    public long WorkspaceId { get; private set; }

    public List<ValidationResult> Errors = [];

    public IDictionary<string, List<ValidationResult>> ValidationResults = new Dictionary<string, List<ValidationResult>>();

    protected ValidationResult AddError(string message, string propertyName)
    {
        if (Errors == null)
            Errors = new List<ValidationResult>();

        var error = new ValidationResult(message, [propertyName]);

        Errors.Add(new ValidationResult(message, [propertyName]));
        return error;
    }

    public virtual bool IsValid => Errors.Count == 0;

    public void Init(Stream stream)
    {
        Workbook = new XLWorkbook(stream);
    }

    public void SetWorkspaceId(long workspaceId)
    {
        WorkspaceId = workspaceId;
    }

    public void SetAppDb(ModuleDbContext appDb)
    {
        AppDb = appDb;
    }

    public virtual void Begin()
    {

    }
    public abstract void Run();
    public virtual void Finally()
    {

    }

    public abstract void SerializeData<T>(string sheetName, object parent, PropertyInfo parentProp, string propertyKey);
    public abstract void ValidateData(string sheetName, Type obj);

    public abstract IServiceProvider SetServiceProvider(IServiceProvider provider);
}

public abstract class BaseImportHandler<TModel> : BaseImportHandler where TModel : IImportable
{
    //private MorphDbContext _mainDb { get; set; }
    //protected MorphDbContext MainDb => _mainDb ??= ServiceProvider.GetRequiredService<MorphDbContext>();

    //private AppDbContext _appDb { get; set; }
    //protected AppDbContext AppDb => _appDb ??= ServiceProvider.GetRequiredService<AppDbContext>();

    //private Identity.DbSchema.Workspace _workspace { get; set; }
    //protected Identity.DbSchema.Workspace Workspace => _workspace ??= MainDb.Workspaces.FirstOrDefault(p => p.Id == WorkspaceId);

    //protected EmployeeView Employee
    //{
    //    get
    //    {
    //        var key = $"__employee_{AppUser.Id}";
    //        if (MemoryCache.Get<Entities.Views.EmployeeView>(key) == null)
    //        {
    //            var employee = AppDb.Views.FirstOrDefaultAsync<Entities.Views.EmployeeView>(new { UserId = AppUser.Id }).Result;
    //            if (employee == null)
    //                return default;

    //            return MemoryCache.Set(key, employee);
    //        }

    //        return MemoryCache.Get<Entities.Views.EmployeeView>(key);
    //    }
    //}
    //protected User AppUser => UserManager.CurrentUser;

    //private UserManager userManager { get; set; }
    //protected UserManager UserManager => userManager ??= GetService<UserManager>();

    //private Auth auth { get; set; }
    //protected Auth Auth => auth ??= GetService<Auth>();

    protected long UserId { get; set; }

    private IMemoryCache _memoryCache { get; set; }
    protected IMemoryCache MemoryCache => _memoryCache ??= GetService<IMemoryCache>();

    protected IServiceProvider ServiceProvider { get; private set; }

    protected T GetService<T>() => ServiceProvider.GetRequiredService<T>();

    public List<TModel> List { get; set; } = [];
    public IDictionary<string, IList> PropList { get; set; } = new Dictionary<string, IList>();

    public override IServiceProvider SetServiceProvider(IServiceProvider provider)
    {
        return ServiceProvider = provider;
    }

    public override bool IsValid => Errors.Count == 0;

    public T SerializeRow<T>(Type type, IXLRow xlRow, PropertyInfo[] props, IDictionary<string, string[]> multicolumns)
    {
        var data = Activator.CreateInstance(type);
        string formatDate = "dd/MM/yyyy";
        try
        {
            #region Validate Cell Value
            int cellIdx = 0;
            for (int j = 0; j < props.Length; j++)
            {
                var column = props[j].GetCustomAttribute<ColumnInfo>();
                cellIdx++;

                var cell = xlRow.Cell(cellIdx);

                XLCellValue cellValue = cell.Value;

                if (cell.HasComment)
                    cell.GetComment().Delete();

                if (column?.Type == "MULTIPLE")
                {
                    var li = new List<string>();
                    var ccc = 0;
                    foreach (var row in multicolumns[props[j].Name])
                    {
                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.None;
                        cell.Style.Fill.BackgroundColor = XLColor.NoColor;
                        var xcel = xlRow.Cell(cellIdx + ccc++);
                        li.Add(xcel.Value.ToString());
                    }
                    props[j].SetValue(data, li);
                    cellIdx += multicolumns[props[j].Name].Length - 1;
                }
                else
                {
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.None;
                    cell.Style.Fill.BackgroundColor = XLColor.NoColor;

                    if (props[j].PropertyType == typeof(bool) || props[j].PropertyType == typeof(bool?))
                    {
                        if (!cellValue.ToString().IsBoolean())
                            continue;

                        props[j].SetValue(data, cellValue.ToString().ToBoolean());
                    }
                    else if (props[j].PropertyType == typeof(long) || props[j].PropertyType == typeof(long?))
                    {
                        if (!cellValue.ToString().IsLong())
                            continue;

                        props[j].SetValue(data, cellValue.ToString().ToLong());
                    }
                    else if (props[j].PropertyType == typeof(double) || props[j].PropertyType == typeof(double?))
                    {
                        if (!cellValue.ToString().IsDouble())
                            continue;

                        props[j].SetValue(data, cellValue.ToString().ToDouble());
                    }
                    else if (props[j].PropertyType == typeof(int) || props[j].PropertyType == typeof(int?))
                    {
                        if (!cellValue.ToString().IsInt())
                            continue;

                        props[j].SetValue(data, cellValue.ToString().ToInt32());
                    }
                    else if (props[j].PropertyType == typeof(byte) || props[j].PropertyType == typeof(byte?))
                    {
                        if (!cellValue.ToString().IsInt16())
                            continue;

                        props[j].SetValue(data, cellValue.ToString().ToByte());
                    }
                    else if (props[j].PropertyType == typeof(DateTime) || props[j].PropertyType == typeof(DateTime?))
                    {
                        AssignDateTimeValue(data, props[j], cellValue);
                    }
                    else if (props[j].PropertyType == typeof(EpochDateTime))
                    {
                        if (cellValue.IsBlank)
                            continue;

                        if (!cellValue.ToString().IsDateTime(formatDate))
                            continue;

                        var yearOnly = props[j].GetCustomAttribute<YearOnly>();
                        var monthYearOnly = props[j].GetCustomAttribute<MonthYearOnly>();

                        if (cellValue.IsDateTime)
                        {
                            var date = cellValue.GetDateTime();
                            if (yearOnly != null)
                            {
                                var d = new DateTime(date.Year, 1, 1, 0, 0, 0);
                                props[j].SetValue(data, d.ToUnixTimeMilliseconds());
                            }
                            else if (monthYearOnly != null)
                            {
                                var d = new DateTime(date.Year, date.Month, 1, 0, 0, 0);
                                props[j].SetValue(data, d.ToUnixTimeMilliseconds());
                            }
                            else
                            {
                                props[j].SetValue(data, date.ToUnixTimeMilliseconds());
                            }
                            continue;
                        }

                        if (cellValue.IsNumber)
                        {
                            var date = DateTime.FromOADate(cellValue.GetNumber());
                            if (yearOnly != null)
                            {
                                var c = cellValue.GetNumber();
                                var d = new DateTime(c.ToInt32(), 1, 1, 0, 0, 0);
                                props[j].SetValue(data, d.ToUnixTimeMilliseconds());
                            }
                            else if (monthYearOnly != null)
                            {
                                var d = new DateTime(date.Year, date.Month, 1, 0, 0, 0);
                                props[j].SetValue(data, d.ToUnixTimeMilliseconds());
                            }
                            else
                            {
                                props[j].SetValue(data, date.ToUnixTimeMilliseconds());
                            }
                            continue;
                        }

                        props[j].SetValue(data, cellValue.ToString().ToDateTime(formatDate).ToUnixTimeMilliseconds());
                    }
                    else if (props[j].PropertyType == typeof(EpochDateTime?))
                    {
                        if (cellValue.IsBlank)
                            continue;

                        var yearOnly = props[j].GetCustomAttribute<YearOnly>();
                        var monthYearOnly = props[j].GetCustomAttribute<MonthYearOnly>();

                        if (cellValue.IsDateTime)
                        {
                            var date = new EpochDateTime(cellValue.GetDateTime().ToUnixTimeMilliseconds());
                            if (yearOnly != null)
                            {
                                var d = new DateTime(date.Year, 1, 1, 0, 0, 0);
                                props[j].SetValue(data, d.ToUnixTimeMilliseconds());
                            }
                            else if (monthYearOnly != null)
                            {
                                var d = new DateTime(date.Year, date.Month, 1, 0, 0, 0);
                                props[j].SetValue(data, d.ToUnixTimeMilliseconds());
                            }
                            else
                            {
                                props[j].SetValue(data, date);
                            }
                            continue;
                        }

                        if (cellValue.IsNumber)
                        {
                            var c = cellValue.GetNumber();

                            var date = (EpochDateTime)DateTime.FromOADate(c).ToUnixTimeMilliseconds();

                            if (yearOnly != null)
                            {
                                var d = new DateTime(c.ToInt32(), 1, 1, 0, 0, 0);
                                props[j].SetValue(data, d.ToUnixTimeMilliseconds());
                            }
                            else if (monthYearOnly != null)
                            {
                                var d = new DateTime(date.Year, date.Month, 1, 0, 0, 0);
                                props[j].SetValue(data, d.ToUnixTimeMilliseconds());
                            }
                            else
                            {
                                props[j].SetValue(data, date);
                            }
                            continue;
                        }

                        if (!cellValue.ToString().IsNullableDateTime(formatDate))
                            continue;

                        props[j].SetValue(data, cellValue.ToString().ToNullableDateTime(formatDate)?.ToUnixTimeMilliseconds());
                    }
                    else
                    {
                        AssignStringValue(data, props[j], cellValue);
                    }
                }
            }
                #endregion

        }
        catch { }

        return (T)data;
    }

    public override void ValidateData(string sheetName, Type obj)
    {
        var sheet = Workbook.Worksheet(sheetName);

        var props = obj.GetProperties().Where(p => !p.GetCustomAttributes(true).Any(c => c.GetType() == typeof(SheetAttribute)) && !p.GetCustomAttributes(true).Any(c => c.GetType() == typeof(NotMappedAttribute))).ToArray();

        var list = PropList[sheetName];

        var method = GetType().GetMethod("ValidateRow", [typeof(int), obj]);

        for(int i = 0; i < list.Count; i++)
        {
            var listKey = $"{sheetName}[{i}]";
            if (!ValidationResults.ContainsKey(listKey))
                ValidationResults.Add(listKey, []);

            #region validate from attributes
            var valResults = list[i].TryValidateRecursive(ServiceProvider);

            ValidationContext vc = new(list[i]);
            vc.InitializeServiceProvider(p => ServiceProvider.GetService(p));
            List<ValidationResult> results = [];
            bool isValid = Validator.TryValidateObject(list[i], vc, results, true);
            if (!isValid)
            {
                var colIdx = 0;
                for (int j = 0; j < props.Length; j++)
                {
                    var cell = sheet.Cell(i + 2, ++colIdx);
                    var cellValue = cell.Value;

                    var column = props[j].GetCustomAttribute<ColumnInfo>();

                    var x = results.Where(p => p.MemberNames.Contains(props[j].Name));
                    foreach (var xa in x)
                    {
                        var error = AddError(xa.ErrorMessage, props[j].Name);
                        cell.AddErrorMessage(error.ErrorMessage);
                    }

                    if (column?.Type == "MULTIPLE")
                    {
                        var cc = AppDb.List($"Select Name from {column.Entity} order by Id");
                        colIdx += cc.Rows.Count - 1;
                    }
                }

                ValidationResults[listKey].AddRange(results);
            }
            #endregion

            if(method != null)
            {
                var validateRowResults = new List<ValidationResult>();

                validateRowResults = (List<ValidationResult>)method.Invoke(this, new object[] { i - 1, list[i] });

                if (validateRowResults.Any())
                {
                    ValidationResults[listKey].AddRange(validateRowResults);

                    var cellIdx = 1;
                    for (int j = 0; j < props.Length; j++)
                    {
                        var cell = sheet.Row(i + 2).Cell(cellIdx);
                        var column = props[j].GetCustomAttribute<ColumnInfo>();

                        if (column?.Type == "MULTIPLE")
                        {
                            var cc = AppDb.List($"Select Name from {column.Entity} order by Id");
                            for (int xx = 0; xx < cc.Rows.Count; xx++)
                            {
                                cell = sheet.Row(i + 2).Cell(cellIdx);
                                var x = validateRowResults.Where(p => p.MemberNames.Contains($"{props[j].Name}_{xx}"));
                                foreach (var xa in x)
                                {
                                    AddError(xa.ErrorMessage, $"{props[j].Name}_{xx}");
                                    cell.AddErrorMessage(xa.ErrorMessage);
                                }
                                cellIdx++;
                            }
                            cellIdx--;
                        }
                        else
                        {
                            var x = validateRowResults.Where(p => p.MemberNames.Contains(props[j].Name));
                            foreach (var xa in x)
                            {
                                AddError(xa.ErrorMessage, props[j].Name);
                                cell.AddErrorMessage(xa.ErrorMessage);
                            }
                        }
                        cellIdx++;
                    }
                }
            }
        }

        var sprops = obj.GetProperties().Where(p => p.GetCustomAttributes(true).Any(c => c.GetType() == typeof(SheetAttribute))).ToArray();

        for (int i = 0; i < sprops.Length; i++)
        {
            var type = sprops[i].PropertyType.GenericTypeArguments[0];
            ValidateData(sprops[i].Name, type);
        }
    }

    public override void SerializeData<T>(string sheetName, object parent, PropertyInfo parentProp, string propertyKey)
    {
        var sheet = Workbook.Worksheet(sheetName);
        var obj = typeof(T);

        //string formatDate = "dd/MM/yyyy";
        var rows = sheet.Rows().ToList();
        var props = obj.GetProperties().Where(p => !p.GetCustomAttributes(true).Any(c => c.GetType() == typeof(SheetAttribute)) && !p.GetCustomAttributes(true).Any(c => c.GetType() == typeof(NotMappedAttribute))).ToArray();

        var multilColumns = new Dictionary<string, string[]>();

        var multicols = props.Where(p => p.GetCustomAttributes(true).FirstOrDefault(p => p.GetType() == typeof(ColumnInfo)) is ColumnInfo { Type: "MULTIPLE" });
        foreach (var col in multicols)
        {
            var column = col.GetCustomAttributes(true).FirstOrDefault(p => p.GetType() == typeof(ColumnInfo)) as ColumnInfo;

            var aadd = AppDb.List($"Select Name from {column.Entity} order by Id");
            List<string> ada = [];
            foreach (DataRow a in aadd.Rows)
            {
                ada.Add(a["Name"].ToString());
            }

            multilColumns.Add(col.Name, [.. ada]);
        }

        T[] cList = new T[rows.Count - 1];

        //for (int i = 1; i < rows.Count; i++)
        //{
        //    bool isEmptyRow = false;
        //    for (int j = 1; j < props.Length; j++)
        //    {
        //        isEmptyRow = string.IsNullOrWhiteSpace(rows[i].Cell(j).Value.ToString());
        //        if (!isEmptyRow)
        //            break;
        //    }

        //    if (isEmptyRow)
        //        continue;

        //    var serializedData = SerializeRow<T>(obj, rows[i], props, multilColumns);

        //    cList[i - 1] = serializedData;
        //}

        Parallel.For(1, rows.Count, (i, state) =>
        {
            bool isEmptyRow = false;
            for (int j = 1; j < props.Length; j++)
            {
                isEmptyRow = string.IsNullOrWhiteSpace(rows[i].Cell(j).Value.ToString());
                if (!isEmptyRow)
                    break;
            }

            if (isEmptyRow)
                state.Break();

            var serializedData = SerializeRow<T>(obj, rows[i], props, multilColumns);

            cList[i - 1] = serializedData;
        });

        if (obj == typeof(TModel)) 
        {
            List = [.. (cList as TModel[])];
            PropList.Add(sheet.Name, List);
        }
        else
        {
            IList list = parent as IList;

            var propKey = parent.GetType().GenericTypeArguments[0].GetProperty(propertyKey);

            cList = cList.Where(p => p != null).ToArray();
            foreach (var r in list)
            {
                if (r == null) continue;
                var parameter = Expression.Parameter(obj, "x");

                var tt = r.GetType().GetProperty(propertyKey);
                var val = Expression.Constant(propKey.GetValue(r));

                var xx = Expression.Equal(Expression.MakeMemberAccess(parameter, obj.GetMember(propertyKey)[0]), val);

                var predicate = Expression.Lambda<Func<T, bool>>(xx, parameter).Compile();

                var xxx = cList.Where(predicate).ToList();

                parentProp.SetValue(r, xxx);
            }

            PropList.Add(sheet.Name, cList.ToList());
        }

        var sprops = obj.GetProperties().Where(p => p.GetCustomAttributes(true).Any(c => c.GetType() == typeof(SheetAttribute))).ToArray();
        
        if (sprops == null) return;

        for (int i = 0; i < sprops.Length; i++)
        {
            if (sprops[i].PropertyType.IsGenericTypeDefinition) continue;

            var sheetInfo = sprops[i].GetCustomAttribute<SheetAttribute>();

            MethodInfo method = GetType().BaseType.GetMethod("SerializeData");
            MethodInfo generic = method.MakeGenericMethod(sprops[i].PropertyType.GenericTypeArguments[0]);

            generic.Invoke(this, [sprops[i].Name , List, sprops[i], sheetInfo.PropertyKey]);
        }
    }

    private void AssignStringValue(object obj, PropertyInfo prop, XLCellValue cellValue)
    {
        if (cellValue.IsBlank) return;

        if (cellValue.IsDateTime)
        {
            if (prop.GetCustomAttribute<TimeFormat>() != null)
            {
                var xx = Convert.ToDateTime(cellValue.ToString()).ToString("hh:mm");
                prop.SetValue(obj, Convert.ToDateTime(cellValue.ToString()).ToString("hh:mm"));
            }
            else
            {
                prop.SetValue(obj, cellValue.ToString());
            }
            return;
        }

        if (cellValue.IsNumber)
        {
            string val = cellValue.GetNumber().ToString("#.#");
            prop.SetValue(obj, val);
            return;
        }

        prop.SetValue(obj, cellValue.ToString());
    }

    private void AssignDateTimeValue(object obj, PropertyInfo prop, XLCellValue cellValue)
    {
        if (cellValue.IsBlank)
            return;

        string formatDate = "dd/MM/yyyy";

        var yearOnly = prop.GetCustomAttribute<YearOnly>();
        var monthYearOnly = prop.GetCustomAttribute<MonthYearOnly>();

        if (cellValue.IsDateTime)
        {
            var date = cellValue.GetDateTime();
            if (yearOnly != null)
            {
                var d = new DateTime(date.Year, 1, 1, 0, 0, 0);
                prop.SetValue(obj, d);
                return;
            }

            if (monthYearOnly != null)
            {
                var d = new DateTime(date.Year, date.Month, 1, 0, 0, 0);
                prop.SetValue(obj, d);
                return;
            }

            prop.SetValue(obj, date);
            return;
        }

        if (cellValue.IsNumber)
        {
            var date = DateTime.FromOADate(cellValue.GetNumber());
            if (yearOnly != null)
            {
                var c = cellValue.GetNumber();
                var d = new DateTime(c.ToInt32(), 1, 1, 0, 0, 0);
                prop.SetValue(obj, d);
                return;
            }

            if (monthYearOnly != null)
            {
                var d = new DateTime(date.Year, date.Month, 1, 0, 0, 0);
                prop.SetValue(obj, d);
                return;
            }

            prop.SetValue(obj, date);
            return;
        }

        if (!cellValue.ToString().IsDateTime(formatDate))
            return;

        prop.SetValue(obj, cellValue.ToString().ToDateTime(formatDate));
    }
}

public static class ImportExtension
{
    public static List<Type> ImportHandlers { get; set; } = [];
    public static List<Type> ImportTemplates { get; set; } = [];
    public static List<Type> ExportTemplates { get; set; } = [];

    public static void AddImportTemplate(Type type)
    {
        ImportTemplates.Add(type);
    }

    public static void AddImportHandler(Type type)
    {
        ImportHandlers.Add(type);
    }

    public static void AddExportTemplate(Type type)
    {
        ExportTemplates.Add(type);
    }

    public static void AddErrorMessage(this IXLCell cell, string message)
    {
        cell.Style.Fill.BackgroundColor = XLColor.Pink;
        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Dashed;
        cell.Style.Border.OutsideBorderColor = XLColor.Red;
        cell.AddNewLineText(message);
    }

    public static void AddNewLineText(this IXLCell cell, string text)
    {
        cell.GetComment().AddNewLine().AddText(text);
    }
}