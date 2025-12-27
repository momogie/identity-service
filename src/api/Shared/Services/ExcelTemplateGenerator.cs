//using ClosedXML.Excel;
//using ClosedXML.Graphics;
//using Org.BouncyCastle.Bcpg.Sig;
//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations.Schema;
//using System.Data;
//using System.IO;
//using System.Linq;
//using System.Reflection;

//namespace Shared;

//public class ExcelTemplateGenerator
//{
//    protected Type TemplateType { get; set; }

//    protected AppDbContext AppDb { get; set; }

//    protected XLWorkbook Workbook { get; set; }

//    public ExcelTemplateGenerator(appdb)
//    {
//        AppDb = appDb;

//        var ttfFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Fonts", "Microsoft Sans Serif", "micross.ttf");
//        if(File.Exists(ttfFile))
//        {
//            using(var strm = File.OpenRead(ttfFile))
//            {
//                LoadOptions.DefaultGraphicEngine = DefaultGraphicEngine.CreateWithFontsAndSystemFonts(strm);
//            }
//        }
//    }

//    public XLWorkbook Generate(Type templateType)
//    {
//        TemplateType = templateType;

//        var importableType = templateType.GetCustomAttributes(typeof(ImportableAttribute), true).FirstOrDefault() as ImportableAttribute;

//        Workbook = new XLWorkbook();
       
//        var worksheet = Workbook.Worksheets.Add(importableType?.SheetName ?? TemplateType.Name);

//        var props = TemplateType.GetProperties().Where(p => !p.GetCustomAttributes(true).Any(c => c.GetType() == typeof(SheetAttribute)) && !p.GetCustomAttributes(true).Any(c => c.GetType() == typeof(NotMappedAttribute)));

//        int idx = 1;
//        foreach (var r in props)
//        {
//            var column = r.GetCustomAttributes(typeof(ColumnInfo), true).FirstOrDefault() as ColumnInfo;
//            var propOption = r.GetCustomAttributes(typeof(PropertyOptions), true).FirstOrDefault() as PropertyOptions;
//            var displayAttribute = r.GetCustomAttributes(true).FirstOrDefault(p => p.GetType() == typeof(DisplayAttribute)) as DisplayAttribute;

//            if (column == null || column?.Type == "STATIC")
//            {
//                var cell = worksheet.Row(1).Cell(idx);
//                cell.Value = column?.Name ?? displayAttribute?.Name ?? r.Name;

//                cell.Style.Font.SetBold();
//                cell.Style.Border.SetBottomBorder(XLBorderStyleValues.Thin);


//                SetColor(cell, column);

//                SetRequiredComment(cell, props, r, column);

//                SetDataType(cell, r);


//                if (!string.IsNullOrWhiteSpace(column?.Description))
//                {
//                    cell.AddNewLineText("Description: " + column.Description);
//                }

//                if (propOption is PropertyOptions)
//                {
//                    cell.AddNewLineText("Options Value: (" + string.Join(", ", propOption.Options) + ")");
//                }


//                //worksheet.Column(idx).AdjustToContents(); // <== error linux, ribet harus install font
//                worksheet.Column(idx).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

//                idx++;
//            }

//            if (column?.Type == "MULTIPLE")
//            {
//                var cc = AppDb.List($"Select Name from {column.Entity} order by Id");
//                foreach (DataRow row in cc.Rows)
//                {
//                    var cell = worksheet.Row(1).Cell(idx);
//                    cell.Value = row["Name"].ToString();

//                    cell.Style.Font.SetBold();
//                    cell.Style.Border.SetBottomBorder(XLBorderStyleValues.Thin);

//                    SetColor(cell, column);

//                    if (r.PropertyType.IsGenericType)
//                    {
//                        Type itemType = r.PropertyType.GetGenericArguments()[0];

//                        cell.GetComment().AddText("Data Type: string");
//                    }

//                    if (!string.IsNullOrWhiteSpace(column?.Description))
//                    {
//                        cell.GetComment().AddNewLine();
//                        cell.GetComment().AddText("Description: " + column.Description);
//                    }

//                    if (propOption is PropertyOptions)
//                    {
//                        cell.GetComment().AddNewLine();
//                        cell.GetComment().AddText("Options Value: " + string.Join(", ", propOption.Options));
//                    }

//                    //worksheet.Column(idx).AdjustToContents(); // <== error linux, ribet harus install font
//                    worksheet.Column(idx).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

//                    idx++;
//                }
//            }
//        }

//        var list = TemplateType.GetProperties().Where(p => p.GetCustomAttributes(true).Any(c => c.GetType() == typeof(SheetAttribute)));
//        foreach (PropertyInfo item in list)
//        {
//            if (item.PropertyType.GenericTypeArguments.Length == 0)
//                continue;

//            var worksheet2 = Workbook.Worksheets.Add(item.Name);

//            int idxx = 1;
//            var props2 = item.PropertyType.GenericTypeArguments[0].GetProperties().Where(p => !p.GetCustomAttributes(true).Any(c => c.GetType() == typeof(SheetAttribute)) && !p.GetCustomAttributes(true).Any(c => c.GetType() == typeof(NotMappedAttribute)));

//            foreach (var r in props2)
//            {
//                var column = r.GetCustomAttributes(typeof(ColumnInfo), true).FirstOrDefault() as ColumnInfo;
//                var propOption = r.GetCustomAttributes(typeof(PropertyOptions), true).FirstOrDefault() as PropertyOptions;
//                var displayAttribute = r.GetCustomAttributes(true).FirstOrDefault(p => p.GetType() == typeof(DisplayAttribute)) as DisplayAttribute;

//                if (column == null || column?.Type == "STATIC")
//                {
//                    var cell = worksheet2.Row(1).Cell(idxx);
//                    cell.Value = column?.Name ?? displayAttribute?.Name ?? r.Name;

//                    cell.Style.Font.SetBold();
//                    cell.Style.Border.SetBottomBorder(XLBorderStyleValues.Thin);

//                    SetColor(cell, column);

//                    SetRequiredComment(cell, props2, r, column);

//                    SetDataType(cell, r);

//                    if (!string.IsNullOrWhiteSpace(column?.Description))
//                    {
//                        cell.GetComment().AddNewLine();
//                        cell.GetComment().AddText("Description: " + column.Description);
//                    }

//                    if (propOption is PropertyOptions)
//                    {
//                        cell.GetComment().AddNewLine();
//                        cell.GetComment().AddText("Options Value: " + string.Join(", ", propOption.Options));
//                    }

//                    //worksheet2.Column(idxx).AdjustToContents(); // <== error linux, ribet harus install font
//                    worksheet2.Column(idxx).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

//                    idxx++;
//                }

//                if (column?.Type == "MULTIPLE")
//                {
//                    var cc = AppDb.List($"Select Name from {column.Entity} order by Id");
//                    foreach (DataRow row in cc.Rows)
//                    {
//                        var cell = worksheet2.Row(1).Cell(idxx);
//                        cell.Value = row["Name"].ToString();

//                        cell.Style.Font.SetBold();
//                        cell.Style.Border.SetBottomBorder(XLBorderStyleValues.Thin);

//                        SetColor(cell, column);

//                        if (r.PropertyType.IsGenericType)
//                        {
//                            Type itemType = r.PropertyType.GetGenericArguments()[0];

//                            cell.GetComment().AddText("Data Type: " + itemType.Name);
//                        }

//                        if (!string.IsNullOrWhiteSpace(column?.Description))
//                        {
//                            cell.GetComment().AddNewLine();
//                            cell.GetComment().AddText("Description: " + column.Description);
//                        }

//                        if (propOption is PropertyOptions)
//                        {
//                            cell.GetComment().AddNewLine();
//                            cell.GetComment().AddText("Options Value: " + string.Join(", ", propOption.Options));
//                        }

//                        //worksheet2.Column(idxx).AdjustToContents(); // <== error linux, ribet harus install font
//                        worksheet2.Column(idxx).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

//                        idxx++;
//                    }
//                }
//            }

//            worksheet2.Columns().AdjustToContents();
//        }

//        worksheet.Columns().AdjustToContents();

//        return Workbook;
//    }

//    protected void SetColumn(IXLCell cell)
//    {
//        cell.Style.Font.SetFontSize(20);
//    }

//    protected void SetColor(IXLCell cell, ColumnInfo column)
//    {
//        // Background Color
//        if (column != null && !string.IsNullOrWhiteSpace(column?.HexBgColor))
//            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml(column.HexBgColor));
//        else
//            cell.Style.Fill.SetBackgroundColor(XLColor.FromArgb(1, 3, 157, 227));

//        // Fore Color
//        if (column != null && !string.IsNullOrWhiteSpace(column?.HexForeColor))
//            cell.Style.Font.SetFontColor(XLColor.FromHtml(column.HexForeColor));
//        else
//            cell.Style.Font.SetFontColor(XLColor.White);
//    }

//    protected void SetRequiredComment(IXLCell cell, IEnumerable<PropertyInfo> properties, PropertyInfo prop,  ColumnInfo column)
//    {
//        var required = prop.GetCustomAttributes(false).FirstOrDefault(p => p.GetType() == typeof(RequiredAttribute)) as RequiredAttribute;
//        if (required != null)
//        {
//            cell.GetComment().AddText("Required");
//            cell.GetComment().AddNewLine();
//        }

//        var requiredIfEqual = prop.GetCustomAttributes(false).FirstOrDefault(p => p.GetType() == typeof(RequiredIfEqual)) as RequiredIfEqual;
//        if (requiredIfEqual != null)
//        {
//            var compareProp = properties.FirstOrDefault(p => p.Name == requiredIfEqual.CompareProperty);
//            var col = compareProp.GetCustomAttributes(typeof(ColumnInfo), true).FirstOrDefault() as ColumnInfo;
//            var dispAttr = compareProp.GetCustomAttributes(true).FirstOrDefault(p => p.GetType() == typeof(DisplayAttribute)) as DisplayAttribute;
//            cell.GetComment().AddText($"Required if column \"{col?.Name ?? dispAttr?.Name ?? requiredIfEqual.CompareProperty}\" is equal \"{requiredIfEqual.Value}\"");
//            cell.GetComment().AddNewLine();
//        }

//        var requiredIfTrue = prop.GetCustomAttributes(false).FirstOrDefault(p => p.GetType() == typeof(RequiredIfTrue)) as RequiredIfTrue;
//        if (requiredIfTrue != null)
//        {
//            var compareProp = properties.FirstOrDefault(p => p.Name == requiredIfTrue.CompareProperty);
//            var col = compareProp.GetCustomAttributes(typeof(ColumnInfo), true).FirstOrDefault() as ColumnInfo;
//            var dispAttr = compareProp.GetCustomAttributes(true).FirstOrDefault(p => p.GetType() == typeof(DisplayAttribute)) as DisplayAttribute;
//            cell.GetComment().AddText($"Required if column \"{col?.Name ?? dispAttr?.Name ?? requiredIfTrue.CompareProperty}\" is equal \"TRUE\"");
//            cell.GetComment().AddNewLine();
//        }

//        var requiredIfFalse = prop.GetCustomAttributes(false).FirstOrDefault(p => p.GetType() == typeof(RequiredIfFalse)) as RequiredIfFalse;
//        if (requiredIfFalse != null)
//        {
//            var compareProp = properties.FirstOrDefault(p => p.Name == requiredIfFalse.CompareProperty);
//            var col = compareProp.GetCustomAttributes(typeof(ColumnInfo), true).FirstOrDefault() as ColumnInfo;
//            var dispAttr = compareProp.GetCustomAttributes(true).FirstOrDefault(p => p.GetType() == typeof(DisplayAttribute)) as DisplayAttribute;
//            cell.GetComment().AddText($"Required if column \"{col?.Name ?? dispAttr?.Name ?? requiredIfFalse.CompareProperty}\" is equal \"FALSE\"");
//            cell.GetComment().AddNewLine();
//        }
//    }

//    protected void SetDataType(IXLCell cell, PropertyInfo prop)
//    {
//        if (prop.PropertyType == typeof(bool) || prop.PropertyType == typeof(bool?))
//        {
//            cell.AddNewLineText("Data Type: Boolean");
//            cell.AddNewLineText("Options Value: (TRUE, FALSE)");
//        }
//        else if (prop.PropertyType == typeof(DateTime))
//        {
//            cell.AddNewLineText("Data Type: DateTime");
//            var yearOnly = prop.GetCustomAttribute<YearOnly>();
//            var monthYearOnly = prop.GetCustomAttribute<MonthYearOnly>();
//            if (yearOnly != null)
//            {
//                cell.AddNewLineText("Format: yyyy");
//                cell.AddNewLineText($"Example: {EpochDateTime.Now.Year}");
//            }
//            else if (monthYearOnly != null)
//            {
//                cell.AddNewLineText("Format: MM/yyyy");
//                cell.AddNewLineText($"Example: {EpochDateTime.Now.Month}/{EpochDateTime.Now.Year}");
//            }
//            else
//            {
//                cell.AddNewLineText("Format: dd/MM/yyyy or dd/MM/yyyy hh:mm:ss");
//            }
//        }
//        else if (prop.PropertyType == typeof(DateTime?))
//        {
//            cell.AddNewLineText("Data Type: Nullable DateTime");

//            var yearOnly = prop.GetCustomAttribute<YearOnly>();
//            var monthYearOnly = prop.GetCustomAttribute<MonthYearOnly>();
//            var required = prop.GetCustomAttribute<RequiredAttribute>();

//            if (required != null)
//            {
//                if (yearOnly != null)
//                {
//                    cell.AddNewLineText("Format: yyyy");
//                    cell.AddNewLineText($"Example: {EpochDateTime.Now.Year}");
//                }
//                else if (monthYearOnly != null)
//                {
//                    cell.AddNewLineText("Format: MM/yyyy");
//                    cell.AddNewLineText($"Example: {EpochDateTime.Now.Month}/{EpochDateTime.Now.Year}");
//                }
//                else
//                {
//                    cell.AddNewLineText("Format: dd/MM/yyyy or dd/MM/yyyy hh:mm:ss");
//                }
//            }
//            else
//            {
//                if (yearOnly != null)
//                {
//                    cell.AddNewLineText("Format: yyyy or \"Leave Blank\"");
//                    cell.AddNewLineText($"Example: {EpochDateTime.Now.Year}");
//                }
//                else if (monthYearOnly != null)
//                {
//                    cell.AddNewLineText("Format: MM/yyyy or \"Leave Blank\"");
//                    cell.AddNewLineText($"Example: {EpochDateTime.Now.Month}/{EpochDateTime.Now.Year}");
//                }
//                else
//                {
//                    cell.AddNewLineText("Format: dd/MM/yyyy or dd/MM/yyyy hh:mm:ss or \"Leave Blank\"");
//                }
//            }
//        }
//        else if (prop.PropertyType == typeof(int))
//        {
//            cell.AddNewLineText("Data Type: Number [0-9]");
//        }
//        else if (prop.PropertyType == typeof(int?))
//        {
//            var required = prop.GetCustomAttributes(false).FirstOrDefault(p => p.GetType() == typeof(RequiredAttribute)) as RequiredAttribute;
//            if(required != null)
//                cell.AddNewLineText("Data Type: Number [0-9]");
//            else
//                cell.AddNewLineText("Data Type: Number [0-9] or \"Leave Blank\"");
//        }
//        else if (prop.PropertyType == typeof(string))
//        {
//            cell.AddNewLineText("Data Type: Text");
//            var mxLen = prop.GetCustomAttributes(false).FirstOrDefault(p => p.GetType() == typeof(MaxLengthAttribute)) as MaxLengthAttribute;
//            if (mxLen != null)
//                cell.AddNewLineText($"Max Length: {mxLen.Length}");

//            var timeFormat = prop.GetCustomAttributes(false).FirstOrDefault(p => p.GetType() == typeof(TimeFormat)) as TimeFormat;
//            if(timeFormat != null)
//                cell.AddNewLineText($"Format: hh:mm");
//        }
//        else
//        {
//            cell.AddNewLineText("Data Type: " + prop.PropertyType.Name);
//        }
//    }
//}
