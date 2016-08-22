using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Services;

/// <summary>
/// Summary description for IceWS
/// </summary>
[WebService(Namespace = "http://yep.org/")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
[System.Web.Script.Services.ScriptService]
public class IceWS : System.Web.Services.WebService
{
    private static string conStr = ConfigurationManager.ConnectionStrings["IceDB"].ConnectionString;
    public IceWS()
    {
        //Uncomment the following line if using designed components 
        //InitializeComponent(); 
    }



    private string terminateRequst(HttpContext Context){
        Context.Response.Status = "403 Forbidden";
        //the next line is untested - thanks to strider for this line
        Context.Response.StatusCode = 403;
        //the next line can result in a ThreadAbortException
        //Context.Response.End(); 
        Context.ApplicationInstance.CompleteRequest();
        return null;
    
    }
    public string GetSessionWithID(User user)
    {
        if (user.Email == null || user._token == null || "".Equals(user.Email) || "".Equals(user._token))
        {
            terminateRequst(Context);
            // return null; // 0 means no logical session
        }

        string sp = "sp_getSessionWithID";
        var _params = new Dictionary<string, string>();
        _params.Add("param1", "@userAccount");
        _params.Add("value1", user.Email);
        _params.Add("param2", "@sessionID");
        _params.Add("value2", user._token);

        return ConvertTableToJsonList((getTable(sp, _params).Tables[0]));
    }
    private string ValidateUser(User user)
    {
        if(user == null || ("email:"+"first:"+"last:"+"address:"+"password:"+"_token:").Equals(user.toString())){
            return null;
        }
        string sp = "sp_doValidateUser", res = "0";
        Dictionary<string,string> _params = new Dictionary<string, string>();
        _params.Add("param1", "@UserEmail");
        _params.Add("value1", user.Email);

        _params.Add("param2", "@UserPassword");
        _params.Add("value2", user.Password);

        res = ConvertTableToJsonList( getTable(sp, _params).Tables[0] );
        if("0".Equals(res))
        { 
            res = "0";
        }
        return res;
    }

    [WebMethod]
    public string DoAjax(string func, string user)
    {
        //user = "{\"Email\":\"pini@c.com,\",\"First\":\"Pini\",\"Last\":\"Ke\",\"Address\":\"Kadima\",\"Password\":\"\",\"City\":\"\",\"_token\":\"519\"}";
        var _user = (User)(new JavaScriptSerializer()).Deserialize(user, typeof(User));
        if (!isAjaxRequestValidate(func,_user))
        {
            Context.Response.StatusDescription = "מזה החרא הזה?אגקס זה נוזל כלים בכלל!";
            Context.Response.StatusCode = 403;
            return null;
        }
        return (new Method(func,_user)).Invok();
    }

    private bool isAjaxRequestValidate(string func,User user)
    {
        if (func == null || "".Equals(func.ToString()))
        {
            return false;
        }
        if (func == "returnSession")
        {
            if ("".Equals(user.Email) || "".Equals(user.Password))
            {
                return false;
            }
        }

        return true;
    }
    
    
    public string GetSession(User user)
    {
        if (user.Email == null || user.Password == null || "".Equals(user.Email) || "".Equals(user.Password))
        {
            return terminateRequst(Context);        
        } // 0 means no logical session
        if ("0".Equals(this.ValidateUser(user)))
        {
            return terminateRequst(Context);
        }

        string sp = "sp_getSession";
        Dictionary<string,string> _params = new Dictionary<string,string>();
        _params.Add("param1","@UserEmail");
        _params.Add("value1",user.Email.ToString());
        return ConvertTableToJsonList(getTable(sp,_params).Tables[0]);
      }

    [WebMethod]
    public string GetYepBranches()
    {
        string sp = "sp_getBranches";
        return ConvertTableToJsonList(getTable(sp,null).Tables[0]);
    }

    public string GetUserOrders(User user)
    {
        var param = new Dictionary<string,string>();
        param.Add("param1", "@UserEmail");
        param.Add("value1", user.Email);
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

public class User
{
    public User(){
        this.Email = "";
        this.First = "";
        this.Last = "";
        this.Address = "";
        this.Password = "";
        this._token = "";
    }
    public string Email
    {
        get;
        set;
    }
    public string First
    {
        get;
        set;
    }
    public string Last
    {
        get;
        set;
    }
    public string Address
    {
        set;
        get;
    }
    public string Password
    {
        get;
        set;
    }
    public string City
    {
        get;
        set;
    }
    public string _token
    {
        get;
        set;
    }
    public string toString()
    {
        return "email:" + this.Email + "first:" + this.First + "last:" + this.Last + "address:" + this.Address + "password:" + this.Password + "_token" + this._token;
    }
}


public class Method{
    const int VLIDATE_SESSION_FUNCTION = 1;
    const int GET_SESSION = 2;
    const int GET_USER_ORDERS = 3;
    const int GET_YEP_BRANCHES = 4;
    public User user = new User();
    Dictionary<string, int> functionKey = new Dictionary<string, int>()
    {
        {"getLoginStatus", VLIDATE_SESSION_FUNCTION},
        {"returnSession", GET_SESSION},
        {"getUserOrders",GET_USER_ORDERS},
        {"funcGetBranches",GET_YEP_BRANCHES}
    };

    public Method(string func,User user)
    {
        this.user.Email = user.Email;
        this.user.First = user.First;
        this.user.Last = user.Last;
        this.user.Address = user.Address;
        this.user._token = user._token;
        this.user.Password = user.Password;
        this._func = func;
    }
    public string _func
    {
        get;set;
    }
    
    public string Invok(){

        switch(functionKey[this._func]){
            case 1:
                return (new IceWS()).GetSessionWithID(this.user);
            case 2:
                    return (new IceWS()).GetSession(this.user);
            case 3:
                    return (new IceWS()).GetUserOrders(this.user);
            case 4:
                    return (new IceWS()).GetYepBranches();
        }
        return "";
    }
}



