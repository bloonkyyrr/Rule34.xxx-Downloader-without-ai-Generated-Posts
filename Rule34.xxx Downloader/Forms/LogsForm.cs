using R34Downloader.Services;
using System;
using System.Windows.Forms;

namespace R34Downloader.Forms
{
    /// <summary>
    /// Download logs viewer form.
    /// </summary>
    public partial class LogsForm : Form
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the LogsForm class.
        /// </summary>
        public LogsForm()
        {
            InitializeComponent();
        }

        #endregion

        #region Handlers

        private void LogsForm_Load(object sender, EventArgs e)
        {
            SetupDataGridView();
            RefreshLogs();
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            RefreshLogs();
        }

        private void buttonClearLogs_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear all logs?", "Clear Logs", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                DownloadLogService.ClearLogs();
                RefreshLogs();
                labelStatus.Text = "Logs cleared.";
            }
        }

        #endregion

        #region Methods

        private void SetupDataGridView()
        {
            dataGridView1.Columns.Clear();

            // ID Column
            var idColumn = new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "ID",
                DataPropertyName = "Id",
                Width = 80
            };
            dataGridView1.Columns.Add(idColumn);

            // Type Column
            var typeColumn = new DataGridViewTextBoxColumn
            {
                Name = "Type",
                HeaderText = "Type",
                DataPropertyName = "Type",
                Width = 70
            };
            dataGridView1.Columns.Add(typeColumn);

            // Status Column
            var statusColumn = new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Status",
                DataPropertyName = "Status",
                Width = 90
            };
            dataGridView1.Columns.Add(statusColumn);

            // Duration Column
            var durationColumn = new DataGridViewTextBoxColumn
            {
                Name = "Duration",
                HeaderText = "Duration (s)",
                DataPropertyName = "Duration",
                Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "0.00" }
            };
            dataGridView1.Columns.Add(durationColumn);

            // Timestamp Column
            var timestampColumn = new DataGridViewTextBoxColumn
            {
                Name = "Timestamp",
                HeaderText = "Timestamp",
                DataPropertyName = "Timestamp",
                Width = 150,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" }
            };
            dataGridView1.Columns.Add(timestampColumn);
        }

        private void RefreshLogs()
        {
            try
            {
                var logs = DownloadLogService.GetAllLogs();
                dataGridView1.DataSource = logs;

                labelLogCount.Text = $"Total Logs: {logs.Count}";
                labelStatus.Text = "Loaded successfully.";
            }
            catch (Exception ex)
            {
                labelStatus.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Error loading logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}
