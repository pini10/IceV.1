/// <reference path="yepJs.js" />
var _userName = Cookies.get("_userName");
var _userID = Cookies.get("_userId")
var _token = Cookies.get("_token");
var _markers = [];
var map;
var _loginState = getLoginStatus(_userID, _token);
var defualtErr = function(err){alert(err);};


function getLoginStatus(_userID,_token){
    if(_userID == undefined || _userID == "" || _token == "" || _token == undefined)
    {
        return false;
    }
    doAjax("GetSessionWithID",JSON.stringify({UserID: _userID, SessionID:_token}),function(data){
        if (data.d != "") {
            return true;
        }
        return false;
     },
     function(err){ alert(err); } );
}// this function validate session
function doAjax(webService,_data,_function,_functionError){
    var WebServiceURL = "IceWS.asmx";//"http://proj.ruppin.ac.il/cegroup3/prod/IceWS.asmx";
    $.support.cors = true;
    $.ajax({
        url: WebServiceURL+'/'+webService,
        dataType: "json",
        type: "POST",
        data: _data,
        contentType: "application/json; charset=utf-8",
        error: _functionError,
        success: _function
    });

}

$(document).on("pagecreate","#branches",function(){
  //ValidateUser({_token: _token, _userID: _userID});
    doAjax("GetYepBranches", "", function(data) {
        var list = JSON.parse(data.d);
        //init map
        var long = list.Table[0].Longitude;
        var lat = list.Table[0].Latitude;
        var loc = new google.maps.LatLng(lat, long);
        var $branchesSelect = $('#select-branches');
        var myOptions = {
            zoom: 15,
            center: loc
        };
        map = new google.maps.Map(document.getElementById("map-canvas"), myOptions);
        google.maps.event.trigger(map, 'resize');
        map.setZoom(map.getZoom());
        //add branches to select
        for (var i = 0 ; i < list.Table.length; i++) {
            $('<option>').val(list.Table[i].BranchName).text(list.Table[i].BranchName).attr({ Telephone:list.Table[i].Telephone,Manager:list.Table[i].Manager,Weekend: list.Table[i].Weekend, WeekendOpenHours: list.Table[i].WeekendOpenHours, MidWeekOpenHours: list.Table[i].MidWeekOpenHours, MidWeek: list.Table[i].MidWeek, Kosher: list.Table[i].Kosher, Address: list.Table[i].Address, BranchName: list.Table[i].BranchName, Latitude: list.Table[i].Latitude, Longitude: list.Table[i].Longitude }).appendTo($branchesSelect);
            //add markers to map
            _markers.push(new google.maps.Marker({
                position: new google.maps.LatLng( list.Table[i].Latitude,list.Table[i].Longitude),
                map: map,
                title: list.Table[i].Address
            }));
        }
        
        $branchesSelect.on('change', function () {
            var $selectedItem = $(this).find(":selected");
            map.setCenter(new google.maps.LatLng($selectedItem.attr('Latitude'), $selectedItem.attr('Longitude')));
            $('#txtSelectedBranche').text($selectedItem.attr("branchname") + ": ");
            $('#txtSelectedBranchName').text($selectedItem.attr("branchname"));
            $('#txtSelectedAddress').text($selectedItem.attr("address"));
            $("#iconSelectedKosher").text("");
            if ($selectedItem.attr("kosher").toLowerCase() == "true") {
                $('<span style="margin:0px;margin-left:5px;background-color:#66FF8F;" class="ui-btn ui-shadow ui-corner-all ui-icon-check ui-btn-icon-notext"></span>').appendTo($("#iconSelectedKosher"))
            }
            else {
                $('<span style="margin:0px;margin-left:5px;background-color:pink;" class="ui-btn ui-shadow ui-corner-all ui-icon-delete ui-btn-icon-notext"></span>').appendTo($("#iconSelectedKosher"))
            }
            $("#txtMidWeek").text($selectedItem.attr('midweek')).addClass('color-text-gray');
            $('#txtMidWeekOpenHours').text($selectedItem.attr('midweekopenhours')).addClass('color-text-gray');
            $("#txtWeekend").text($selectedItem.attr('weekend')).addClass('color-text-gray');
            $('#txtWeekendOpenHours').text($selectedItem.attr('weekendopenhours')).addClass('color-text-gray');
            $('#txtManager').text($selectedItem.attr('manager')).addClass('color-text-gray');
            $('#txtTelephone').text($selectedItem.attr('telephone')).addClass('color-text-gray');
        });
        $branchesSelect.val($branchesSelect.children().first().attr('value')).change();
    }
 ,defualtErr);
}); //this function handle branches page

function buildPopup(){

    var string;
    string = '<div data-role="popup" id="signIn" class="ui-corner-all" data-position-to="window" data-transition="turn">' +
       '' +
           ' <div class="myPop">' +
               ' <h3>Please sign in</h3>' +
               ' <label for="txtLoginUserName" class="ui-hidden-accessible">Username:</label>' +
                '<input type="text" name="txtLoginUserName" id="txtLoginUserName" value="" placeholder="username" data-theme="a" />' +
                '<label for="txtUserPassword" class="ui-hidden-accessible">Password:</label>' +
                '<input type="password" name="txtLoginPassword" id="txtLoginPassword" value="" placeholder="password" data-theme="a" />' +
                '<a id="btnLogin"  class="ui-btn Mybtn">Sign in</a>' +
            '</div>' +
        '' +
    '</div><!--popup signin-->';
    return string;
}

$(document).one('pagebeforecreate', function () {
    var panel = buildMenu();
    var popup = buildPopup();
    $.mobile.pageContainer.prepend(panel);
    $.mobile.pageContainer.prepend(popup);
    $("#signIn").popup().enhanceWithin();
    $("#btnLogin").on("tap",sendLogin);
    $("#menu").panel().enhanceWithin();


}); //this function is dynamiclly define the panel menu 

$(document).on("pageshow", "#index", function(event){
	sliderStart('#index'); // init slider
	$('#index' + ' .menuLink').attr('href','#menu'); // bind menu button
	setMenu("index"); // init menu
}); //init index page

$(document).on("pageshow", "#branches", function(event){
	sliderStart('#branches');// init slider
	$('#Branches' + ' .menuLink').attr('href','#menu');// bind menu button
	setMenu('branches'); // init menu


}); // init branches page


function setMenu(pageId){

		$("#menu li").each(function(){
		var link = $(this).find("a");
		var liId = $(this).attr('id');
		if (liId != undefined && liId != "") {
		    if (liId.toLowerCase() == pageId) {
		        $(this).attr('data-role', 'list-divider');
		        link.attr('class', 'ui-btn');
		    }
		    else {
		        link.attr('href', '#' + liId.toLowerCase());
		        $(this).removeAttr("data-role").removeAttr('class', '');
		        link.attr('class', 'ui-btn ui-btn-icon-right ui-icon-carat-r');
		    }
		}
	});
         
		if (_loginState != false) {
		    $('#statusBar').append($("<div>").attr({ class: 'ui-block-a', id: 'conUserName' }).append($("<div>").attr({ class: 'ui-block-b', id: 'conStatus' })));
		    $("#conUserName").html('<a data-role="button" class=" ui-btn ui-corner-all ui-shadow">' + _userName + '</a>').buttonMarkup();
		    $("#conStatus").html('<a data-role="button" class=" ui-btn ui-corner-all ui-shadow ">' + 'Log Out ' + '</a>').first().buttonMarkup();
         }
		else {
		    $('#statusBar').parent().attr("style","padding:0px;");
            $('#statusBar').removeClass('ui-grid-a').addClass('ui-grid-solo').html($("<div>").addClass("ui-block-a").attr({ id: 'conStatus' }));
            $("#conStatus").html($("<a>").attr({'data-rel':'popup',href:'#signIn', 'data-role': "button" }).addClass("ui-btn btnSignIn").text("Log In/Sign In").buttonMarkup());
         }
        $("#menuList").listview("refresh");
} // this function recive a pageId and init the menu by this page id.

function LOGOUT() {
    alert("LOGOUT");
}
function sendLogin() {
    var _user = $('#txtLoginUserName').val(), _password = $('#txtLoginPassword').val();
    if (validateLogIn(_user, _password)) {
        _userID = _user;
        doAjax("GetSession", JSON.stringify({ UserID: _userID, Password: _password }), setSession , defualtErr);
    }
}
function setSession(data) {

    data = JSON.parse(data.d);
    _token = data.Table[0].SessionID;
    _userName = data.Table[0].FirstName;
    Cookies.set('_token', _token);
    Cookies.set('_userName', _userName);
    logOnFillDisplay(_userName,_userID,_token);
}
function logOnFillDisplay(_userName, _userID, _token){
    //TODO: open "CLOSED" section in menu, Fill user orders
    $("#menuList").append($('<li id="MYORDERS">').html($("<a>").addClass("ui-btn ui-btn-icon-right ui-icon-carat-r").text("My Orders")));
    fillUserOrders(_userID,_token);
    //doAjax(getUserOrders)<- TODO: display orders section 
}
function fillUserOrders(_userID, _token){
    if (getLoginStatus(_userID, _token)) {//check session
        //session validate
        $("#myOrdersContent").html($('<ul>').attr({ 'data-role': "listview", id:"ordersList" }));
        //doAjax get orders and fill the data.
        doAjax('GetUserOrders', JSON.stringify({ UserId: _userID }),
            function (data) {
                data = JSON.parse(data.d);
                data = data.Table;
                data.each(function () {
                    var $this = $(this);
                    $('#ordersList').append($("<li>").attr({ 'data-role': 'collapsible', 'data-iconpos': 'right', 'data-inset': "false" }).html($("<h2>").text("First Name: " + $this.FirstName + ", Last Name: " + $this.LastName + ", no." + $this.Id).append($("<ul>").attr({ 'data-role': "listview", id: "orderlist" }).html($("<li>").text('Id: '+$this.Id+"<br/>"+"First Name: "+$this.FirstName+", Last Name:"+$this.LastName+"<br/>"+"Telephone: "+$this.PhoneNumber+ ", Date & Time: "+$this.DateTime+"<br/>"+$this.Address)))));

                });

        }, defualtErr);
    }
}
function validateLogIn(_userID, _userPassword) {
    var res = true
    if (_userID == "" || _userID == undefined) {
        if ($('#txtLoginUserName').after().prop('tagName') != 'SPAN') {
            $('#txtLoginUserName').after($('<span class="errorMsg">').text("* User Name is required."));
        }
        res = false;
    }
    else {
        if ($('#txtLoginUserName').after().prop('tagName') == 'SPAN') {
            $('#txtLoginUserName').after().remove();
        }
    }
    if (_userPassword == "" || _userPassword == undefined) {
        if ($('#txtLoginPassword').after().prop('tagName') != 'SPAN') {
            $('#txtLoginPassword').after($('<span  class="errorMsg">').text("* Password is required."));
        }
        res = false;
    }
    else {
        if ($('#txtLoginPassword').after().prop('tagName') == 'SPAN') {
            $('#txtLoginPassword').after().remove();
        }
    }
    return res;
}
function buildMenu() {
	//cheching vars
	var string = '';
	//prepering code
	string += '<div data-role="panel" id="menu" data-position="right" data-display="push" data-theme="a">'+
	'<ul data-role="listview" id="menuList" >' +
    '<li><div id="statusBar" class="ui-grid-a"></div></li>' +
	'<li id="INDEX"><a>Home</a></li>'+
	'<li id="BRANCHES"><a>Branches</a></li>'+
	'</ul>'+
	'</div>';
	return string;
}//this function return a menu as string
function sliderStart(pageIdWithHash){
	var $thisPage = pageIdWithHash;
	$($thisPage + ' .mySlide > div:gt(0)').hide();

	setInterval(function(){
		var $firstDiv = $($thisPage+' .mySlide > div:first');
		$firstDiv.fadeOut(1000);
		$firstDiv.next().fadeIn(1000,function(){
			$firstDiv.appendTo( $thisPage +' .mySlide');	
		});
	},3000);
} // func recive page id and init its slider.


	