using DevExpress.Drawing;
using DevExpress.XtraReports.UI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;

namespace AccountingSystem.Reports.JournalEntry
{
    public partial class JournalEntryReport : DevExpress.XtraReports.UI.XtraReport
    {
        public JournalEntryReport()
        {
            InitializeComponent();
            ApplyReportFonts();
        }
        private const string ReportFontFamily = "Vazirmatn";

        private void ApplyReportFonts()
        {
            Font = new DXFont(ReportFontFamily, Font.Size, Font.Style);

            foreach (var control in GetAllControls(this))
            {
                control.Font = new DXFont(ReportFontFamily, control.Font.Size, control.Font.Style);
            }
        }

        private static IEnumerable<XRControl> GetAllControls(XtraReport report)
        {
            foreach (Band band in report.Bands)
            {
                foreach (var control in GetAllControls(band.Controls))
                {
                    yield return control;
                }
            }
        }

        private static IEnumerable<XRControl> GetAllControls(XRControlCollection controls)
        {
            foreach (XRControl control in controls)
            {
                yield return control;

                foreach (var child in GetAllControls(control.Controls))
                {
                    yield return child;
                }
            }
        }
    }
}
