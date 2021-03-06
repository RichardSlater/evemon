using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using EVEMon.Common.Controls;
using EVEMon.Common.Models;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.SettingsObjects;

namespace EVEMon.SettingsUI
{
    /// <summary>
    /// Maintenance dialog to edit an APIConfiguration instance.
    /// </summary>
    public partial class APISettingsForm : EVEMonForm
    {
        private readonly APIProvidersSettings m_providers;
        private readonly SerializableAPIProvider m_provider;

        public APISettingsForm(APIProvidersSettings providers, SerializableAPIProvider newProvider)
        {
            InitializeComponent();
            m_providers = providers;
            m_provider = newProvider;
        }

        /// <summary>
        /// Overrides System.Windows.Forms.Form.OnLoad()
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Initialize();
        }

        /// <summary>
        /// Form initialisation. Populates and enables the main form elements using the provided APIConfiguration instance.
        /// The Name property of the APIConfiguration instance may only be changed if it has not been previously assigned.
        /// </summary>
        private void Initialize()
        {
            if (m_provider == null)
                return;

            txtConfigurationName.Text = m_provider.Name;
            txtAPIHost.Text = m_provider.Address;
            SupportsCompressedResponseCheckBox.Checked = m_provider.SupportsCompressedResponse;
            InitializeDataGrid();
        }

        /// <summary>
        /// Populates the DataGridView control with APIMethod details from the specified APIConfiguration instance.
        /// The APIMethod is stored in the DataGridViewRow.
        /// <remarks>Tag property for reference.</remarks>
        /// </summary>
        private void InitializeDataGrid()
        {
            dgMethods.Rows.Clear();
            foreach (SerializableAPIMethod method in m_provider.Methods)
            {
                // Fills empty path with the default one
                if (String.IsNullOrEmpty(method.Path))
                    method.Path = APIProvider.DefaultProvider.Methods.First(x => x.Method.ToString() == method.MethodName).Path;

                // Skip method with no path
                if (String.IsNullOrWhiteSpace(method.Path))
                    continue;

                // Add row
                int rowIndex = dgMethods.Rows.Add(method.MethodName, method.Path);
                dgMethods.Rows[rowIndex].Tag = method;
            }
        }

        /// <summary>
        /// Resets the Path column of the Methods DataGridView to the Path property of the APIConfiguration.DefaultMethods collection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUseDefaults_Click(object sender, EventArgs e)
        {
            IEnumerable<APIMethod> defaultMethods = APIProvider.DefaultProvider.Methods;
            foreach (DataGridViewRow row in dgMethods.Rows)
            {
                SerializableAPIMethod rowMethod = (SerializableAPIMethod)row.Tag;
                foreach (APIMethod defaultMethod in defaultMethods.Where(
                    defaultMethod => defaultMethod.Method.ToString() == rowMethod.MethodName))
                {
                    row.Cells[1].Value = defaultMethod.Path;
                }
            }
        }

        /// <summary>
        /// Validates user input and assigns the edited values back to the APIConfiguration instance before closing the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOK_Click(object sender, EventArgs e)
        {
            if (!ValidateChildren())
                return;

            m_provider.Name = txtConfigurationName.Text;
            m_provider.Address = txtAPIHost.Text;
            m_provider.SupportsCompressedResponse = SupportsCompressedResponseCheckBox.Checked;

            foreach (DataGridViewRow row in dgMethods.Rows)
            {
                SerializableAPIMethod method = (SerializableAPIMethod)row.Tag;
                method.Path = (string)row.Cells[1].Value;
            }

            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Validates the Configuration Name input value
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtConfigurationName_Validating(object sender, CancelEventArgs e)
        {
            string configName = txtConfigurationName.Text.Trim();

            // Checks it is not a empty name
            if (String.IsNullOrEmpty(configName))
            {
                ShowValidationError(txtConfigurationName, "Configuration Name cannot be blank.");
                e.Cancel = true;
                return;
            }

            // Check the name does not already exist
            bool exist = configName == APIProvider.DefaultProvider.Name;
            exist = m_providers.CustomProviders.Aggregate(exist,
                                                          (current, provider) =>
                                                          current | (configName == provider.Name && provider != m_provider));

            if (!exist)
                return;

            ShowValidationError(txtConfigurationName, $"There is already a provider named {configName}.");
            e.Cancel = true;
        }

        /// <summary>
        /// Clears a Configuration Name validation error once the input value has been validated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtConfigurationName_Validated(object sender, EventArgs e)
        {
            ClearValidationError(txtConfigurationName);
        }

        /// <summary>
        /// Validates the API Host Name input value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtAPIHost_Validating(object sender, CancelEventArgs e)
        {
            string apiHost = txtAPIHost.Text.Trim();
            if (!String.IsNullOrEmpty(apiHost))
                return;

            ShowValidationError(txtAPIHost, "API Host Name cannot be blank.");
            e.Cancel = true;
        }

        /// <summary>
        /// Clears an API Host validation error once the input value has been validated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtAPIHost_Validated(object sender, EventArgs e)
        {
            ClearValidationError(txtAPIHost);
        }

        /// <summary>
        /// Validates an API Method Path input value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgMethods_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (!String.IsNullOrEmpty((string)e.FormattedValue))
                return;

            ShowValidationError(dgMethods, $"Path for method {dgMethods.Rows[e.RowIndex].Cells[0].Value} cannot be blank");
            e.Cancel = true;
        }

        /// <summary>
        /// Clears an API Method Path validation error once the input value has been validated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgMethods_CellValidated(object sender, DataGridViewCellEventArgs e)
        {
            ClearValidationError(dgMethods);
        }

        /// <summary>
        /// Displays a validation error notification for the specified control using the specified message.
        /// </summary>
        /// <param name="control"></param>
        /// <param name="errorMessage"></param>
        private void ShowValidationError(Control control, string errorMessage)
        {
            errorProvider.SetError(control, errorMessage);
        }

        /// <summary>
        /// Clears a validation error notification on the specified control.
        /// </summary>
        /// <param name="control"></param>
        private void ClearValidationError(Control control)
        {
            errorProvider.SetError(control, String.Empty);
        }
    }
}