using System;
using System.IO;
using System.Windows;
using OfficeOpenXml;

namespace HardwareInserter.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // EPPlus 7.x requiere declarar el contexto de licencia antes de usar ExcelPackage.
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public static string CatalogJsonPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "catalog.json");
    }
}
