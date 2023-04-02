using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using IntersectionSupport;

namespace WaSARxGUI
{
    public partial class Form1 : Form
    {
        private string dataFileName;
        private List<object> geoObjects;
        private System.Windows.Forms.DataGridView dgvResults;

        public Form1()
        {
            InitializeComponent();
            ConfigureDataGridView();
            this.FormBorderStyle = FormBorderStyle.Sizable;
            // Set the Anchor property of the controls
            btnChooseFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnSaveLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvResults.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        }

        private void ConfigureDataGridView()
        {
            dgvResults.Columns.Clear();
            dgvResults.Columns.Add("Area", "Area");
            dgvResults.Columns.Add("Routes", "Routes");
            dgvResults.Columns.Add("Distance", "Distance (km)");
            dgvResults.Columns.Add("TotalLength", "Total Length (km)");
            dgvResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvResults.RowHeadersVisible = false; // Hide the row headers
        }



        private async void btnChooseFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "JSON files (*.json)|*.json|Text files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 3;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    dataFileName = openFileDialog.FileName;
                    lblStatus.Text = "Processing...";
                    UpdateStatusLabel(lblStatus.Text, Color.FromArgb(255, 211, 211, 211));
                    await ProcessDataFileAsync(); // Use 'await' keyword here
                }
            }
        }

        private async Task ProcessDataFileAsync()
        {
            await Task.Run(() => ProcessDataFile());
        }

        private void UpdateStatusLabel(string text, System.Drawing.Color color)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => UpdateStatusLabel(text, color)));
            }
            else
            {
                lblStatus.Text = text;
                lblStatus.ForeColor = color;
            }
        }

        private void UpdateDataGridViewSafely(Action action)
        {
            if (dgvResults.InvokeRequired)
            {
                dgvResults.Invoke(new Action(() => UpdateDataGridViewSafely(action)));
            }
            else
            {
                action();
            }
        }


        private void ProcessDataFile()
        {
            try
            {
                geoObjects = JsonFilter.ParseGeoJsonFile(dataFileName);
                if (geoObjects == null || geoObjects.Count == 0)
                {
                    UpdateStatusLabel("Error", System.Drawing.Color.Red);
                    MessageBox.Show($"Failed to open GeoJSON file. File path: {dataFileName}");
                    return;
                }

                IntersectionSupport.IntersectFind intersectFind = new IntersectionSupport.IntersectFind();
                intersectFind.FindIntersectionsGUI(dataFileName);

                List<AreaResult> areaResults = intersectFind.AreaResults;

                if (areaResults != null)
                {
                    PopulateDataGridView(areaResults);
                }

                UpdateStatusLabel("Success", Color.FromArgb(48, 213, 200));
            }
            catch (Exception ex)
            {
                UpdateStatusLabel("Error", System.Drawing.Color.Red);
                MessageBox.Show($"Error: Failed to process file '{dataFileName}'. {ex.Message}");
            }
        }

        private void PopulateDataGridView(List<AreaResult> areaResults)
        {
            UpdateDataGridViewSafely(() =>
            {
                dgvResults.Rows.Clear();

                if (areaResults != null)
                {
                    foreach (var areaResult in areaResults)
                    {
                        string areaName = areaResult.Name;
                        List<IntersectionResult> intersectionResults = areaResult.IntersectionResults;

                        if (intersectionResults != null)
                        {
                            int rowIndex = -1;
                            double totalDistance = 0;

                            for (int i = 0; i < intersectionResults.Count; i++)
                            {
                                var intersectionResult = intersectionResults[i];
                                rowIndex = dgvResults.Rows.Add();

                                if (i == 0) // If it's the first route, add the area.
                                {
                                    dgvResults.Rows[rowIndex].Cells["Area"].Value = areaName;
                                }

                                dgvResults.Rows[rowIndex].Cells["Routes"].Value = intersectionResult.RouteName;
                                dgvResults.Rows[rowIndex].Cells["Distance"].Value = intersectionResult.Distance.ToString("F2");
                                totalDistance += intersectionResult.Distance;

                                if (i == intersectionResults.Count - 1) // If it's the last route, display the total distance.
                                {
                                    dgvResults.Rows[rowIndex].Cells["TotalLength"].Value = totalDistance.ToString("F2");
                                }
                            }
                        }
                    }
                }
            });
        }

        private void SaveLogToFile(string logFileName)
        {
            using (StreamWriter sw = new StreamWriter(logFileName))
            {
                // Write dataset filename and current date and time
                sw.WriteLine($"Dataset File: {Path.GetFileName(dataFileName)}");
                sw.WriteLine($"Generated at: {DateTime.Now}");
                sw.WriteLine();

                // Write top separator line
                sw.WriteLine(new string('-', 100));

                // Write column headers
                sw.WriteLine(string.Format("{0,-30}|{1,-30}|{2,-20}|{3,-20}", dgvResults.Columns[0].HeaderText, dgvResults.Columns[1].HeaderText, dgvResults.Columns[2].HeaderText, dgvResults.Columns[3].HeaderText));
                sw.WriteLine(new string('-', 100));

                // Write data rows
                string previousArea = string.Empty;
                for (int i = 0; i < dgvResults.RowCount; i++)
                {
                    string area = dgvResults[0, i].Value?.ToString() ?? string.Empty;
                    string route = dgvResults[1, i].Value?.ToString() ?? string.Empty;
                    string distance = dgvResults[2, i].Value?.ToString() ?? string.Empty;
                    string totalLength = dgvResults[3, i].Value?.ToString() ?? string.Empty;

                    // Add separator line between Areas
                    if (i > 0 && area != previousArea)
                    {
                        sw.WriteLine(new string('-', 100));
                    }

                    sw.WriteLine(string.Format("{0,-30}|{1,-30}|{2,-20}|{3,-20}", area, route, distance, totalLength));
                    previousArea = area;
                }

                // Write bottom separator line
                sw.WriteLine(new string('-', 100));
            }
        }

        private void btnSaveLog_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Log files (*.log)|*.log";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string logFileName = saveFileDialog.FileName;
                    SaveLogToFile(logFileName);
                }
            }
        }

    }
}
