using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AddressBookAssignment
{
    public partial class Form1 : Form
    {
        // Instantiate global variables for SQL
        SqlConnection con;
        SqlCommand cmd;
        SqlDataReader reader;
        Connection dbconn = new Connection();
        private int selectedContactId = 0;  // Store the ID of the selected contact

        public Form1()
        {
            InitializeComponent();
            LoadContacts();  // Load existing contacts on form load
        }

        // Method to handle application exit
        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("EXIT APPLICATION?", "EXIT", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        // Method to handle Add New Contact button click
        private void btnAddContact_Click(object sender, EventArgs e)
        {
            panel2.Enabled = true;  // Enable the form panel for adding a new contact
            btnUpdate.Enabled = false;  // Disable update button while adding new contact
            btnClear.Enabled = true;  // Enable clear button
        }

        // Calculate age from birth date
        public int CalculateAge(DateTime birthDate)
        {
            int age = DateTime.Now.Year - birthDate.Year;
            if (DateTime.Now < birthDate.AddYears(age)) age--; // Adjust if birthday hasn't occurred this year
            return age;
        }

        // Validate form inputs before adding a contact
        private bool ValidateForm()
        {
            if (string.IsNullOrEmpty(txtLastName.Text) || string.IsNullOrEmpty(txtFirstName.Text))
            {
                MessageBox.Show("Please enter valid Last and First Name!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrEmpty(txtAge.Text) || !int.TryParse(txtAge.Text, out _))
            {
                MessageBox.Show("Please enter a valid age!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrEmpty(txtEmailAdd.Text) || !Regex.IsMatch(txtEmailAdd.Text, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$"))
            {
                MessageBox.Show("Please enter a valid email address!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrEmpty(txtMobileNo.Text) || !Regex.IsMatch(txtMobileNo.Text, @"^\d{11}$"))
            {
                MessageBox.Show("Please enter a valid 11-digit mobile number!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        // Method to add a new contact
        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;  // If validation fails, exit

            try
            {
                using (con = new SqlConnection(dbconn.MyConnectionDB()))
                {
                    con.Open();
                    string insertQuery = "INSERT INTO TBL_USERPROFILE (LASTNAME, FIRSTNAME, MIDDLENAME, SEX, BIRTHDATE, AGE, CONTACTNO, EMAIL, ADDRESS) " +
                                         "VALUES (@LASTNAME, @FIRSTNAME, @MIDDLENAME, @SEX, @BIRTHDATE, @AGE, @CONTACTNO, @EMAIL, @ADDRESS)";
                    cmd = new SqlCommand(insertQuery, con);
                    AddParametersToCommand();
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("New Contact Added Successfully!", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearText();
                LoadContacts();  // Refresh the contact list
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding contact: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper method to add form data parameters to SQL command
        private void AddParametersToCommand()
        {
            cmd.Parameters.AddWithValue("@LASTNAME", txtLastName.Text);
            cmd.Parameters.AddWithValue("@FIRSTNAME", txtFirstName.Text);
            cmd.Parameters.AddWithValue("@MIDDLENAME", txtMidName.Text);
            cmd.Parameters.AddWithValue("@SEX", cmbSex.Text);
            cmd.Parameters.AddWithValue("@BIRTHDATE", dtpBirthDate.Value);
            cmd.Parameters.AddWithValue("@AGE", txtAge.Text);
            cmd.Parameters.AddWithValue("@CONTACTNO", txtMobileNo.Text);
            cmd.Parameters.AddWithValue("@EMAIL", txtEmailAdd.Text);
            cmd.Parameters.AddWithValue("@ADDRESS", txtAddress.Text);
        }

        // Clear all input fields after adding or updating a contact
        public void ClearText()
        {
            txtLastName.Clear();
            txtFirstName.Clear();
            txtMidName.Clear();
            cmbSex.SelectedIndex = -1;
            dtpBirthDate.Value = DateTime.Now;
            txtAge.Clear();
            txtMobileNo.Clear();
            txtEmailAdd.Clear();
            txtAddress.Clear();
            panel2.Enabled = false;
        }

        // Update the age when the birth date changes
        private void dtpBirthDate_ValueChanged(object sender, EventArgs e)
        {
            txtAge.Text = CalculateAge(dtpBirthDate.Value.Date).ToString();
        }

        // Load contacts into DataGridView
        public void LoadContacts()
        {
            try
            {
                using (con = new SqlConnection(dbconn.MyConnectionDB()))
                {
                    con.Open();
                    dataGridView1.Rows.Clear();  // Clear existing rows
                    cmd = new SqlCommand("SELECT * FROM TBL_USERPROFILE", con);
                    reader = cmd.ExecuteReader();
                    int i = 0;
                    while (reader.Read())
                    {
                        i++;
                        dataGridView1.Rows.Add(i, reader["ID"], reader["FULLNAME"], reader["BIRTHDATE"], reader["AGE"], reader["CONTACTNO"], reader["EMAIL"], reader["ADDRESS"]);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading contacts: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Handle editing and deleting contacts from DataGridView
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                string colName = dataGridView1.Columns[e.ColumnIndex].Name;
                int id = int.Parse(dataGridView1.Rows[e.RowIndex].Cells["ID"].Value.ToString());

                if (colName == "Edit")
                {
                    selectedContactId = id;
                    panel2.Enabled = true;
                    btnAdd.Enabled = false;
                    btnUpdate.Enabled = true;

                    // Load contact details for editing
                    LoadContactDetailsForEditing(id);
                }
                else if (colName == "Delete")
                {
                    // Confirm and delete the contact
                    if (MessageBox.Show("Are you sure you want to delete this contact?", "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        DeleteContact(id);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error processing contact details: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Load contact details for editing
        private void LoadContactDetailsForEditing(int id)
        {
            using (con = new SqlConnection(dbconn.MyConnectionDB()))
            {
                con.Open();
                cmd = new SqlCommand("SELECT * FROM TBL_USERPROFILE WHERE ID = @ID", con);
                cmd.Parameters.AddWithValue("@ID", id);
                reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    txtLastName.Text = reader["LASTNAME"].ToString();
                    txtFirstName.Text = reader["FIRSTNAME"].ToString();
                    txtMidName.Text = reader["MIDDLENAME"].ToString();
                    cmbSex.Text = reader["SEX"].ToString();
                    dtpBirthDate.Value = DateTime.Parse(reader["BIRTHDATE"].ToString());
                    txtAge.Text = reader["AGE"].ToString();
                    txtMobileNo.Text = reader["CONTACTNO"].ToString();
                    txtEmailAdd.Text = reader["EMAIL"].ToString();
                    txtAddress.Text = reader["ADDRESS"].ToString();
                }
            }
        }

        // Delete a contact from the database
        private void DeleteContact(int id)
        {
            try
            {
                using (con = new SqlConnection(dbconn.MyConnectionDB()))
                {
                    con.Open();
                    cmd = new SqlCommand("DELETE FROM TBL_USERPROFILE WHERE ID = @ID", con);
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Contact deleted successfully!", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadContacts();  // Refresh the contact list
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting contact: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Handle updating an existing contact
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (!ValidateForm()) return;  // If validation fails, exit

            try
            {
                using (con = new SqlConnection(dbconn.MyConnectionDB()))
                {
                    con.Open();
                    string updateQuery = "UPDATE TBL_USERPROFILE SET LASTNAME = @LASTNAME, FIRSTNAME = @FIRSTNAME, MIDDLENAME = @MIDDLENAME, " +
                                         "SEX = @SEX, BIRTHDATE = @BIRTHDATE, AGE = @AGE, CONTACTNO = @CONTACTNO, EMAIL = @EMAIL, ADDRESS = @ADDRESS " +
                                         "WHERE ID = @ID";
                    cmd = new SqlCommand(updateQuery, con);
                    AddParametersToCommand();
                    cmd.Parameters.AddWithValue("@ID", selectedContactId);  // Use the stored ID for update
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Contact updated successfully!", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearText();
                LoadContacts();  // Refresh the contact list
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating contact: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Handle search functionality
        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                LoadContacts();  // Reload all contacts if the search term is empty
                return;
            }

            try
            {
                // Search contacts by last name starting with the search term
                string query = "SELECT * FROM TBL_USERPROFILE WHERE LASTNAME LIKE @searchTerm";
                using (con = new SqlConnection(dbconn.MyConnectionDB()))
                {
                    con.Open();
                    cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@searchTerm", txtSearch.Text + "%");

                    reader = cmd.ExecuteReader();
                    dataGridView1.Rows.Clear();
                    int i = 0;
                    while (reader.Read())
                    {
                        i++;
                        dataGridView1.Rows.Add(i, reader["ID"], reader["FULLNAME"], reader["BIRTHDATE"], reader["AGE"], reader["CONTACTNO"], reader["EMAIL"], reader["ADDRESS"]);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error searching contacts: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Clear all fields in the form
        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearText();  // Clear text method
            btnAdd.Enabled = true;
            btnUpdate.Enabled = false;
        }
    }
}
