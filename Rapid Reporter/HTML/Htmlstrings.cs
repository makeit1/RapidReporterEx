namespace Rapid_Reporter.HTML
{
	public static class Htmlstrings
	{
        public static string HtmlTitle = ": Session Report";
        public static string AHtmlHead;
        public static string BTitleOut;
        public static string CStyle;
        public static string DJavascript;
        public static string EBody;
        public static string GTable;
        public static string JTableEnd;
        public static string MHtmlEnd;

        static Htmlstrings()
        {
            AHtmlHead = "<html>\r\n    <head>\r\n        <meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">\r\n        <!--RRExV3.0-->\r\n        <title>";
            BTitleOut = "        </title>";
            CStyle = "\r\n        <style>\r\n            html *\r\n            {\r\n                font-family: Verdana !important;\r\n                font-size: 11px;\r\n            }\r\n            .aroundtable {font-family: Verdana; font-size: 11px; }\r\n            H1 {text-align: center; font-family: Verdana;}\r\n            H5 {text-align: center; font-family: Verdana; font-weight: normal;} \r\n            table {margin-left: auto; margin-right: auto; min-width:700px; width:80%; border-collapse: collapse; border: 1px solid black;}\r\n            table tr img {max-width: 350px; max-height: 250px; resize-scale: showall;}\r\n            table tr td {padding: 2px;}\r\n\r\n            table tr.Session {font-weight: bold; background: #FAFAFA;}\r\n            table tr.Scenario {font-weight: bold; background: #FAFAFA;}\r\n            table tr.Environment {font-weight: bold; background: #FAFAFA;}\r\n            table tr.Versions {font-weight: bold; background: #FAFAFA;}\r\n\r\n            table tr.Bug\\/Issue {background: #FF4D4D;}\r\n            table tr.Follow {background: #5CADFF;}\r\n            table tr.Note {background: #FAFAFA;}\r\n            table tr.Test {background: #FAFAFA;}\r\n            table tr.Success {background: #80FF80;}\r\n            table tr.Prerequisite {background: #FAFAFA;}\r\n            table tr.Summary {background: #FAFAFA;}\r\n\r\n            table tr.Screenshot {background: #FAFAFA;}\r\n            table tr.PlainText {background: #FAFAFA;}\r\n\r\n            table td.notetype {font-weight: bold; width:190px;}\r\n            table td.timestamp {font-weight: bold; width:175px;}\r\n        </style>";
            DJavascript = "\r\n        <script>\r\n            function ShowImgEle(eleId, bigImgId, littleImgId)\r\n            {\r\n                var sessionTable = document.getElementById('aroundtable');\r\n\t            sessionTable.style.display = \"none\";\r\n\t            var bigImgDiv = document.getElementById(eleId);\r\n\t            bigImgDiv.style.display = \"inline\";\r\n\t            //bigImgDiv.style.background = \"url('\"+img64+\"') no-repeat\";\r\n\t            //bigImgDiv.style.backgroundPosition = \"0px 15px\";\r\n\t            bigImgDiv.style.textDecoration = \"underline\";\r\n                var bigImg = document.getElementById(bigImgId);\r\n                var littleImg = document.getElementById(littleImgId);\r\n                bigImg.src = littleImg.src;\r\n            }\r\n            function HideImgEle(eleId)\r\n            {\r\n\t            var bigImgDiv = document.getElementById(eleId);\r\n                var sessionTable = document.getElementById('aroundtable');\r\n\t            sessionTable.style.display = \"inline\";\r\n\t            bigImgDiv.style.display = \"none\";\r\n            }\r\n            function ShowPlaintextNote(eleId)\r\n            {\r\n\t            var ele = document.getElementById(eleId);\r\n                var eletable = document.getElementById('aroundtable');\r\n\t            eletable.style.display = \"none\";\r\n\t            ele.style.display = \"inline\";\r\n            }\r\n            function HidePlaintextNote(eleId)\r\n            {\r\n\t            var ele = document.getElementById(eleId);\r\n                var eletable = document.getElementById('aroundtable');\r\n\t            eletable.style.display = \"inline\";\r\n\t            ele.style.display = \"none\";\r\n            }\r\n        </script>\r\n    </head>";
            EBody = "<body>\r\n        <div id=\"allbody\">\r\n            <h1>";
            GTable = "\r\n            </h1>\r\n            <!--[if IE]><h5>For best results, use Chrome or Firefox.</h5><![endif]-->\r\n            <div id=\"aroundtable\">\r\n                <table border=\"1\">\r\n";
            JTableEnd = "\r\n                </table>\r\n            </div>\r\n";
            MHtmlEnd = "\r\n        </div>\r\n    </body>\r\n</html>\r\n";
        }
	}
}