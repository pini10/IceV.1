using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Services;

/// <summary>
/// Summary description for IceWS
/// </summary>
[WebService(Namespace = "http://tempuri.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
[System.Web.Script.Services.ScriptService]
public class IceWS : System.Web.Services.WebService
{
    string item = "jhu";
    private static string conStr = ConfigurationManager.ConnectionStrings["IceDB"].ConnectionString;
    public IceWS()
    {
        //Uncomment the following line if using designed components 
        //InitializeComponent(); 
    }

    //[WebMethod]
    //public string[] CurrentDateAndTime()
    //{
    //    string[] time = new string[2];
    //    time[0] = DateTime.Now.ToShortTimeString();
    //    time[1] = DateTime.Now.ToShortDateString();
    //    return time;
    //}

    [WebMethod]
    public string GetSessionWithID(string UserID, string SessionID)
    {
        if (UserID == null || SessionID == null || "".Equals(UserID) || "".Equals(SessionID))
            return null; // 0 means no logical session


        string sp = "sp_getSessionWithID", res = null;
        using (SqlConnection con = new SqlConnection(conStr))
        {
            using (SqlCommand cmd = new SqlCommand(sp, con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@userAccount", UserID);
                cmd.Parameters.AddWithValue("@sessionID", SessionID);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                    res = dr[0].ToString();
                dr.Close();
            }
        }
        return res;
    }
    private string ValidateUser(string UserID, string Password)
    {
        string sp = "sp_doValidateUser", res = "0";
        using (SqlConnection con = new SqlConnection(conStr))
        {
            using (SqlCommand cmd = new SqlCommand(sp, con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@userAccount", UserID);
                cmd.Parameters.AddWithValue("@userPassword", Password);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                    res = dr[0].ToString();
            }
        }
        return res;
    }
    [WebMethod]
    public string GetSession(string UserID, string Password)
    {
        if (UserID == null || Password == null || "".Equals(UserID) || "".Equals(Password))
            return null; // 0 means no logical session
        if ("0".Equals(this.ValidateUser(UserID, Password)))
            return null;

        string sp = "sp_getSession";
        Dictionary<string,string> _params = new Dictionary<string,string>();
        _params.Add("param1","@userAccount");
        _params.Add("value1",UserID.ToString());
        return ConvertTableToJsonList(getTable(sp,_params).Tables[0]);
      }

    
    [WebMethod]
    public string GetYepBranches()
    {
        string sp = "sp_getBranches";
        return ConvertTableToJsonList(getTable(sp,null).Tables[0]);
    }

    [WebMethod]
    public string GetUserOrders(string UserId)
    {
        var param = new Dictionary<string,string>();
        param.Add("value1",UserId);
        param.Add("param1", "@userAccount");
        string sp = "sp_getOrders";
        return ConvertTableToJsonList(getTable(sp, param).Tables[0]);
    }


    [WebMethod]
    public int addProduct(string ProductName)
    {
        int rows = 0;
        string sp = "sp_addProduct";
        SqlConnection con = new SqlConnection(conStr);
        SqlCommand com = new SqlCommand(sp, con);
        com.CommandType = CommandType.StoredProcedure;
        com.Parameters.AddWithValue("@ProductName", ProductName);
        com.Connection.Open();
        rows = com.ExecuteNonQuery();
        return rows;
    }

    [WebMethod]
    public List<Yep> GetProducts()
    {
        string sp = "sp_getProducts";
        return ConvertDataToString(getTable(sp,null));
    }

    [WebMethod]
    public int addFlavor(string Flavor)
    {
        int rows = 0;
        string sp = "sp_addFlavor";
        SqlConnection con = new SqlConnection(conStr);
        SqlCommand com = new SqlCommand(sp, con);
        com.CommandType = CommandType.StoredProcedure;
        com.Parameters.AddWithValue("@Flavor", Flavor);
        com.Connection.Open();
        rows = com.ExecuteNonQuery();
        return rows;
    }

    [WebMethod]
    public List<Yep> GetFlavors()
    {
        string sp = "sp_getFlavors";
        return ConvertDataToString(getTable(sp,null));
    }


    private DataSet getTable(string storedProcedure,Dictionary<string,string> param)
    {
        //DataTable dt = new DataTable();
        DataSet ds = new DataSet();//
        using (SqlConnection con = new SqlConnection(conStr))
        {
            using (SqlCommand cmd = new SqlCommand(storedProcedure, con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                if (param != null)
                {
                    for (var i = 1; i <= param.Count/2; i++ )
                    {
                        cmd.Parameters.AddWithValue(param["param"+i], param["value"+i]);
                    }
                }
                con.Open();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(ds);//
                //da.Fill(dt);
            }
        }
        return ds;
    }


    private string ConvertTableToJsonList(DataTable Dt)
    {
        string[] StrDc = new string[Dt.Columns.Count];
        string HeadStr = string.Empty;

        for (int i = 0; i < Dt.Columns.Count; i++)
        {
            StrDc[i] = Dt.Columns[i].Caption;
            HeadStr += "\"" + StrDc[i] + "\" : \"" + StrDc[i] + i.ToString() + "¾" + "\",";
        }

        HeadStr = HeadStr.Substring(0, HeadStr.Length - 1);

        StringBuilder Sb = new StringBuilder();
        Sb.Append("{\"" + Dt.TableName + "\" : [");

        for (int i = 0; i < Dt.Rows.Count; i++)
        {
            string TempStr = HeadStr;
            Sb.Append("{");

            for (int j = 0; j < Dt.Columns.Count; j++)
            {
                TempStr = TempStr.Replace(Dt.Columns[j] + j.ToString() + "¾", Dt.Rows[i][j].ToString());
            }
            Sb.Append(TempStr + "},");
        }

        Sb = new StringBuilder(Sb.ToString().Substring(0, Sb.ToString().Length - 1));
        Sb.Append("]}");

        return Sb.ToString();
    }

    private List<Yep> ConvertDataToString(DataSet ds)
    {
        //System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
        List<Yep> rows = new List<Yep>();
        //Dictionary<string, object> row;
        //Yep row;
        foreach (DataTable dt in ds.Tables)
        {
            foreach (DataRow dr in dt.Rows)
            {
                Yep row;
               // row = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    row = new Yep(col.ColumnName, dr[col]);
                    //row.Add(col.ColumnName, dr[col]);
                    rows.Add(row);
                }
                
            }
        }
        return rows;
    }
}

public class Yep
{
    public Yep()
    {
    }
    public string colName { get; set; }
    public object value { get; set; }
    public Yep (string colName, object value) { this.colName = colName; this.value = value; }
}




