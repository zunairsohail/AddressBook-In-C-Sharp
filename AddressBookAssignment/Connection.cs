using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddressBookAssignment
{
    public class Connection
    {
        public string MyConnectionDB()
        {
            string conn = "Data Source=DESKTOP-40LD0F2\\SQLEXPRESS;Initial Catalog=AddressBook_DB;Integrated Security=True;TrustServerCertificate=True";
       return conn; 
        }
    }
}
