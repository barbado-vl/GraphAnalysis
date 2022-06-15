
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using GraphAnalysis.DataModel;


namespace GraphAnalysis
{
    /// <summary>
    /// Логика взаимодействия для RecordWindow.xaml
    /// </summary>
    public partial class RecordWindow : Window
    {
        public RecordWindow()
        {
            InitializeComponent();
        }

        public void RecordMethod(List<TLine> cells)
        {
            foreach (TLine cell in cells)
            {
                // Динамика касания, начало

                if (cell.Way > 1)
                {
                    TextRange rangeOfDinam = new(docBox.Document.ContentEnd, docBox.Document.ContentEnd);

                    rangeOfDinam.Text = ">";
                    rangeOfDinam.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily("Calibri"));
                    rangeOfDinam.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                    rangeOfDinam.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Baseline);
                }

                // MainType и CommonType

                TextRange rangeOfMainCommonT = new(docBox.Document.ContentEnd, docBox.Document.ContentEnd);

                rangeOfMainCommonT.Text = cell.CommonType ? "о" + cell.MainType : cell.MainType;

                rangeOfMainCommonT.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily("Calibri"));
                rangeOfMainCommonT.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Baseline);

                if (cell.Way is 1 or 3) rangeOfMainCommonT.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Gray);
                else if (cell.CommonType) rangeOfMainCommonT.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.LimeGreen);
                else
                {
                    if (cell.MainType is "Лн") rangeOfMainCommonT.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.LightBlue);
                    else if (cell.MainType is "Ор") rangeOfMainCommonT.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Firebrick);
                    else if (cell.MainType is "Рэ") rangeOfMainCommonT.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkBlue);
                }

                // Если Близость для Основного Типа "б/д"

                if (cell.PreviousProximity[1] != null && (cell.PreviousProximity[1].Contains('б') || cell.PreviousProximity[1].Contains('д')))
                {
                    TextRange rangeOfMainProx = new(docBox.Document.ContentEnd, docBox.Document.ContentEnd);

                    rangeOfMainProx.Text = cell.PreviousProximity[1];
                    rangeOfMainProx.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily("Calibri"));

                    if (cell.MainType is "Лн") rangeOfMainProx.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.LightBlue);
                    else if (cell.MainType is "Ор") rangeOfMainProx.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Firebrick);
                    else if (cell.MainType is "Рэ") rangeOfMainProx.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkBlue);

                    rangeOfMainProx.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Superscript);
                }

                // Если Близость для Типа Общий "б/д"

                if (cell.PreviousProximity[2] != null && (cell.PreviousProximity[2].Contains('б') || cell.PreviousProximity[2].Contains('д')))
                {
                    TextRange rangeOfСommonProx = new(docBox.Document.ContentEnd, docBox.Document.ContentEnd);

                    rangeOfСommonProx.Text = cell.PreviousProximity[2];
                    rangeOfСommonProx.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily("Calibri"));
                    rangeOfСommonProx.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.LimeGreen);
                    rangeOfСommonProx.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Superscript);
                }

                // VectorType

                if (cell.VectorType is not null or "")
                {
                    TextRange rangeOfVector = new(docBox.Document.ContentEnd, docBox.Document.ContentEnd);

                    rangeOfVector.Text = cell.VectorType;
                    rangeOfVector.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily("Calibri"));
                    rangeOfVector.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                    rangeOfVector.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Subscript);
                }

                // HistoryType и близость по ней если "б/д"

                double cellhis = cell.HistoryType.Last().ToString() is "п" ? 2.5 : double.Parse(cell.HistoryType.Split("С").Last());

                if (cellhis != 2)
                {
                    TextRange rangeOfHistory = new(docBox.Document.ContentEnd, docBox.Document.ContentEnd);

                    rangeOfHistory.Text = cell.HistoryType;

                    if (cellhis > 2 && cell.PreviousProximity[3] != null && (cell.PreviousProximity[3].Contains('б') || cell.PreviousProximity[3].Contains('д')))
                        rangeOfHistory.Text += cell.PreviousProximity[3];

                    rangeOfHistory.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily("Calibri"));
                    rangeOfHistory.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.DarkGray);
                    rangeOfHistory.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Superscript);
                }

                // Динамика касания, продолжение, и Близость между линиями вообще

                TextRange rangeOfbaseProx = new(docBox.Document.ContentEnd, docBox.Document.ContentEnd);

                rangeOfbaseProx.Text = "";

                if (cell.Way is 1 or 3)
                    rangeOfbaseProx.Text += ">";
                if (cell.NextProximity[0] != null && (cell.NextProximity[0].Contains('-') || cell.NextProximity[0].Contains('=')))
                    rangeOfbaseProx.Text += cell.NextProximity[0];
                else
                    rangeOfbaseProx.Text += ", ";

                rangeOfbaseProx.ApplyPropertyValue(TextElement.FontFamilyProperty, new FontFamily("Calibri"));
                rangeOfbaseProx.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                rangeOfbaseProx.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Baseline);
            }

        }
    }
}
