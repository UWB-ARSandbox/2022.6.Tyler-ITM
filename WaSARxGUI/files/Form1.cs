using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using IntersectionSupport;
using Newtonsoft.Json;

namespace WaSARxGUI
{
    public partial class Form1 : Form
    {
        private string dataFileName;
        private List<object> geoObjects;
        private System.Windows.Forms.DataGridView dgvResults;
        private List<AreaResult> areaResults;
        private bool sortByRoutes = false;



        public Form1()
        {
            InitializeComponent();
            ConfigureDataGridView();
            this.FormBorderStyle = FormBorderStyle.Sizable;
            // Set the Anchor property of the controls
            btnChooseFile.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            lblTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnSaveLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            dgvResults.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            // Attach a handler for the form's Resize event
            this.Resize += Form1_Resize;
            AlignButtons();

            btnSortByRoutes = new Button();
            btnSortByRoutes.Text = "Sort by Routes";
            btnSortByRoutes.Size = btnChooseFile.Size; // Set the Sort by Routes button size equal to the Choose File button size
            btnSortByRoutes.Anchor = AnchorStyles.Top | AnchorStyles.Right; // Change the anchor to the right side
            btnSortByRoutes.Click += BtnSortByRoutes_Click;
            btnSortByRoutes.BackColor = Color.White; // Set the button color to white
            btnSortByRoutes.FlatAppearance.BorderSize = 0; // Remove the white padding by setting the border size to 0
            btnSortByRoutes.Visible = true; // Set the button to be initially invisible
            Controls.Add(btnSortByRoutes);
            AlignButtons();


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

        // 
        private void AlignButtons()
        {
            int buttonRightMargin = 10;
            int buttonMargin = 10;

            btnChooseFile.Left = this.ClientSize.Width - btnChooseFile.Width - buttonRightMargin;
            btnSaveLog.Left = this.ClientSize.Width - btnSaveLog.Width - buttonRightMargin;

            btnSortByRoutes.Left = btnChooseFile.Left - btnSortByRoutes.Width - buttonMargin; // Update the position of the Sort by Routes button
            btnSortByRoutes.Top = btnChooseFile.Top;
        }



        // New event handler for the form's Resize event
        private void Form1_Resize(object sender, EventArgs e)
        {
            AlignButtons();
        }

        private void ProcessDataFile()
        {
            try
            {
                if (!string.Equals(Path.GetExtension(dataFileName), ".json", StringComparison.OrdinalIgnoreCase)
    && !string.Equals(Path.GetExtension(dataFileName), ".txt", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateStatusLabel("Error", System.Drawing.Color.Red);
                    MessageBox.Show($"Invalid file type. Please select a JSON or TXT file.");
                    return;
                }


                List<object> geoObjects;
                try
                {
                    geoObjects = JsonFilter.ParseGeoJsonFile(dataFileName);
                }
                catch (JsonReaderException ex)
                {
                    UpdateStatusLabel("Error", System.Drawing.Color.Red);
                    MessageBox.Show($"Error: Failed to parse GeoJSON file '{dataFileName}'. Error message: {ex.Message}. Line {ex.LineNumber}, position {ex.LinePosition}.");
                    return;
                }

                if (geoObjects == null || geoObjects.Count == 0)
                {
                    UpdateStatusLabel("Error", System.Drawing.Color.Red);
                    MessageBox.Show($"Failed to open GeoJSON file. File path: {dataFileName}");
                    return;
                }

                IntersectionSupport.IntersectFind intersectFind = new IntersectionSupport.IntersectFind();
                intersectFind.FindIntersectionsGUI(dataFileName);

                areaResults = intersectFind.AreaResults;

                if (areaResults != null)
                {
                    PopulateDataGridView(areaResults);
                }


                UpdateStatusLabel("Success", Color.FromArgb(48, 213, 200));
                //say where the detailed log is saved and name
                //log file include Sorted by Area && Sorted by Team
            }
            catch (Exception ex)
            {
                UpdateStatusLabel("Error", System.Drawing.Color.Red);
                MessageBox.Show($"Error: Failed to process file '{dataFileName}'. {ex.Message}\n{ex.StackTrace}");
            }
        }
        private void PopulateDataGridView(List<AreaResult> areaResults)
        {
            UpdateDataGridViewSafely(() =>
            {
                dgvResults.Rows.Clear();
                dgvResults.Columns["Area"].DisplayIndex = 0; // Set the display index of the Area column to 0
                dgvResults.Columns["Routes"].DisplayIndex = 1; // Set the display index of the Routes column to 1

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
                            }

                            // Add a row for the total length
                            rowIndex = dgvResults.Rows.Add();
                            dgvResults.Rows[rowIndex].Cells["Area"].Value = string.Empty;
                            dgvResults.Rows[rowIndex].Cells["Routes"].Value = "Total Length:";
                            dgvResults.Rows[rowIndex].Cells["TotalLength"].Value = totalDistance.ToString("F2");
                            dgvResults.Rows[rowIndex].DefaultCellStyle.BackColor = Color.LightGray;
                        }
                    }
                }
            });
        }


        private void BtnSortByRoutes_Click(object sender, EventArgs e)
        {
            sortByRoutes = !sortByRoutes; // Toggle the flag

            if (sortByRoutes)
            {
                btnSortByRoutes.Text = "Sort by Area"; // Change button text to reflect the next sorting method
                PopulateDataGridViewByRoutes(areaResults);
            }
            else
            {
                btnSortByRoutes.Text = "Sort by Routes"; // Change button text back to the original sorting method
                PopulateDataGridView(areaResults);

            }
        }

        private void ShowSortByRoutesButton()
        {
            if (btnSortByRoutes.InvokeRequired)
            {
                btnSortByRoutes.Invoke(new Action(ShowSortByRoutesButton));
            }
            else
            {
                btnSortByRoutes.Visible = true;
            }
        }

        private void PopulateDataGridViewByRoutes(List<AreaResult> areaResults)
        {
            UpdateDataGridViewSafely(() =>
            {
                dgvResults.Rows.Clear();
                dgvResults.Columns["Area"].DisplayIndex = 1; // Set the display index of the Area column to 1
                dgvResults.Columns["Routes"].DisplayIndex = 0; // Set the display index of the Routes column to 0

                if (areaResults != null)
                {
                    Dictionary<string, List<(string Area, double Distance)>> routesDictionary = new Dictionary<string, List<(string Area, double Distance)>>();

                    foreach (var areaResult in areaResults)
                    {
                        string areaName = areaResult.Name;
                        List<IntersectionResult> intersectionResults = areaResult.IntersectionResults;

                        if (intersectionResults != null)
                        {
                            for (int i = 0; i < intersectionResults.Count; i++)
                            {
                                var intersectionResult = intersectionResults[i];
                                string routeName = intersectionResult.RouteName;
                                double distance = intersectionResult.Distance;

                                if (routesDictionary.ContainsKey(routeName))
                                {
                                    routesDictionary[routeName].Add((areaName, distance));
                                }
                                else
                                {
                                    routesDictionary[routeName] = new List<(string Area, double Distance)> { (areaName, distance) };
                                }
                            }
                        }
                    }

                    foreach (var routeEntry in routesDictionary)
                    {
                        string routeName = routeEntry.Key;
                        List<(string Area, double Distance)> areas = routeEntry.Value;
                        double totalDistance = areas.Sum(a => a.Distance);

                        for (int i = 0; i < areas.Count; i++)
                        {
                            int rowIndex = dgvResults.Rows.Add();
                            if (i == 0) // Only display the route name in the first row
                            {
                                dgvResults.Rows[rowIndex].Cells["Routes"].Value = routeName;
                            }
                            dgvResults.Rows[rowIndex].Cells["Area"].Value = areas[i].Area;
                            dgvResults.Rows[rowIndex].Cells["Distance"].Value = areas[i].Distance.ToString("F2"); // Show individual area lengths in the Distance column
                        }

                        // Add a row for the total length and highlight it in grey
                        int totalLengthRowIndex = dgvResults.Rows.Add();
                        dgvResults.Rows[totalLengthRowIndex].Cells["Area"].Value = "Total Length:";
                        dgvResults.Rows[totalLengthRowIndex].Cells["TotalLength"].Value = totalDistance.ToString("F2");
                        dgvResults.Rows[totalLengthRowIndex].DefaultCellStyle.BackColor = Color.LightGray;
                    }
                }

            });
        }

        private void WriteDataGridViewToStream(DataGridView dataGridView, StreamWriter streamWriter, string outputType)
        {
            string previousArea = string.Empty;
            for (int i = 0; i < dataGridView.RowCount; i++)
            {
                string area = dataGridView[0, i].Value?.ToString() ?? string.Empty;
                string route = dataGridView[1, i].Value?.ToString() ?? string.Empty;
                string distance = dataGridView[2, i].Value?.ToString() ?? string.Empty;
                string totalLength = dataGridView[3, i].Value?.ToString() ?? string.Empty;

                if (outputType == "Sorted by Area" && !string.IsNullOrEmpty(totalLength))
                {
                    // Print the row with the total length label in the first column and the value in the fourth column for "Sorted by Area"
                    streamWriter.WriteLine(string.Format("{0,-30}|{1,-30}|{2,-20}|{3,-20}", "Total Length:", string.Empty, string.Empty, totalLength));
                }
                else
                {
                    // Print the row with the total length in the last column for "Sorted by Routes"
                    streamWriter.WriteLine(string.Format("{0,-30}|{1,-30}|{2,-20}|{3,-20}", area, route, distance, totalLength));
                }

                // Add separator line after each row
                streamWriter.WriteLine(new string('-', 100));

                previousArea = area;
            }
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

                // Write Sorted by Area output
                sw.WriteLine("Sorted by Area");
                sw.WriteLine(new string('-', 100));
                WriteDataGridViewToStream(dgvResults, sw, "Sorted by Area");
                sw.WriteLine();

                // Write Sorted by Routes output
                sw.WriteLine("Sorted by Routes");
                sw.WriteLine(new string('-', 100));
                PopulateDataGridViewByRoutes(areaResults);
                WriteDataGridViewToStream(dgvResults, sw, "Sorted by Routes");
                sw.WriteLine();

                // Reset the DataGridView back to the Sorted by Area output
                PopulateDataGridView(areaResults);

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
                    MessageBox.Show($"Output saved at: {logFileName}", "Output Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

    }
}
