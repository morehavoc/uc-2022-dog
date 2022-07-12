function httpGet(theUrl)
{
    var xmlHttp = new XMLHttpRequest();
    xmlHttp.open( "GET", theUrl, false ); // false for synchronous request
    xmlHttp.send( null );
    return xmlHttp.responseText;
}
function sendAction (e) {
    console.log(e);
    var r = httpGet("/action?action="+e);
    console.log(r);
}