using System;
using System.Text;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Elmah;
using umbraco.BasePages;

namespace Terabyte.Umbraco.Elmah
{
		/// <summary>
		/// Copied and modified from original src to support UmbracoEnsuredPage
		/// And tweaked output:
		///		remove links to xml & json
		///		include original page inline. ( casues invalid markup tho :-\ )
		/// http://code.google.com/p/elmah/source/browse/tags/REL-1.1/src/Elmah/ErrorDetailPage.cs
		/// </summary>
		public class ElmahDetail : UmbracoEnsuredPage
		{
			protected Literal Output;

			private ErrorLogEntry _errorEntry;

			protected void Page_Load(object sender, EventArgs e)
			{
				//
				// Retrieve the ID of the error to display and read it from
				// the store.
				//

				string errorId = this.Request.QueryString["id"];

				if (errorId.Length == 0)
					return;

				var log = ErrorLog.GetDefault(HttpContext.Current);
				_errorEntry = log.GetError(errorId);

				//
				// Perhaps the error has been deleted from the store? Whatever
				// the reason, bail out silently.
				//

				if (_errorEntry == null)
				{
					Response.StatusCode = (int)HttpStatusCode.NotFound;
					return;
				}

				//
				// Setup the title of the page.
				//

				this.Title = string.Format("Error: {0} [{1}]", _errorEntry.Error.Type, _errorEntry.Id);

			}

			protected void Page_PreRender(object sender, EventArgs e)
			{
				StringBuilder htmlString = new StringBuilder(); // this will hold the string
				StringWriter stringWriter = new StringWriter(htmlString);
				HtmlTextWriter writer = new HtmlTextWriter(stringWriter);

				if (writer == null)
					throw new ArgumentNullException("writer");

				if (_errorEntry != null)
					RenderError(writer);
				else
					RenderNoError(writer);

				Output.Text = htmlString.ToString();
			}

			private static void RenderNoError(HtmlTextWriter writer)
			{
				//Debug.Assert(writer != null);

				writer.RenderBeginTag(HtmlTextWriterTag.P);
				writer.Write("Error not found in log.");
				writer.RenderEndTag(); // </p>
				writer.WriteLine();
			}

			private void RenderError(HtmlTextWriter writer)
			{
				//Debug.Assert(writer != null);

				Error error = _errorEntry.Error;

				//
				// Write out the page title containing error type and message.
				//

				writer.AddAttribute(HtmlTextWriterAttribute.Id, "PageTitle");
				writer.RenderBeginTag(HtmlTextWriterTag.H1);
				Server.HtmlEncode(error.Message, writer);
				writer.RenderEndTag(); // </h1>
				writer.WriteLine();

				//            SpeedBar.Render(writer,
				//                SpeedBar.Home.Format(BasePageName),
				//                SpeedBar.Help,
				//                SpeedBar.About.Format(BasePageName));

				writer.AddAttribute(HtmlTextWriterAttribute.Id, "ErrorTitle");
				writer.RenderBeginTag(HtmlTextWriterTag.P);

				writer.AddAttribute(HtmlTextWriterAttribute.Id, "ErrorType");
				writer.RenderBeginTag(HtmlTextWriterTag.Span);
				Server.HtmlEncode(error.Type, writer);
				writer.RenderEndTag(); // </span>

				writer.AddAttribute(HtmlTextWriterAttribute.Id, "ErrorTypeMessageSeparator");
				writer.RenderBeginTag(HtmlTextWriterTag.Span);
				writer.Write(": ");
				writer.RenderEndTag(); // </span>

				writer.AddAttribute(HtmlTextWriterAttribute.Id, "ErrorMessage");
				writer.RenderBeginTag(HtmlTextWriterTag.Span);
				Server.HtmlEncode(error.Message, writer);
				writer.RenderEndTag(); // </span>

				writer.RenderEndTag(); // </p>
				writer.WriteLine();

				//
				// Do we have details, like the stack trace? If so, then write
				// them out in a pre-formatted (pre) element.
				// NOTE: There is an assumption here that detail will always
				// contain a stack trace. If it doesn't then pre-formatting
				// might not be the right thing to do here.
				//

				if (error.Detail.Length != 0)
				{
					writer.AddAttribute(HtmlTextWriterAttribute.Id, "ErrorDetail");
					writer.RenderBeginTag(HtmlTextWriterTag.Pre);
					writer.Flush();
					Server.HtmlEncode(error.Detail, writer.InnerWriter);
					writer.RenderEndTag(); // </pre>
					writer.WriteLine();
				}

				//
				// Write out the error log time. This will be in the local
				// time zone of the server. Would be a good idea to indicate
				// it here for the user.
				//

				writer.AddAttribute(HtmlTextWriterAttribute.Id, "ErrorLogTime");
				writer.RenderBeginTag(HtmlTextWriterTag.P);
				Server.HtmlEncode(string.Format("Logged on {0} at {1}",
					error.Time.ToLongDateString(),
					error.Time.ToLongTimeString()), writer);
				writer.RenderEndTag(); // </p>
				writer.WriteLine();

				//
				// Render alternate links.
				//

				//            writer.RenderBeginTag(HtmlTextWriterTag.P);
				//            writer.Write("See also:");
				//            writer.RenderEndTag(); // </p>
				//            writer.WriteLine();

				//writer.RenderBeginTag(HtmlTextWriterTag.Ul);

				//
				// Do we have an HTML formatted message from ASP.NET? If yes
				// then write out a link to it instead of embedding it
				// with the rest of the content since it is an entire HTML
				// document in itself.
				//

				if (error.WebHostHtmlMessage.Length != 0)
				{
					//writer.RenderBeginTag(HtmlTextWriterTag.Li);
					//string htmlUrl = "/usercontrols/Dashboard/ElmahHtml.aspx?id=" + HttpUtility.UrlEncode(_errorEntry.Id);
					//writer.AddAttribute(HtmlTextWriterAttribute.Href, htmlUrl);
					//writer.RenderBeginTag(HtmlTextWriterTag.A);
					//writer.Write("Original ASP.NET error page");
					//writer.RenderEndTag(); // </a>
					//HACK: changed to show original page inline
					writer.RenderBeginTag(HtmlTextWriterTag.Fieldset);
					writer.AddAttribute(HtmlTextWriterAttribute.Class, "original-error");
					writer.RenderBeginTag(HtmlTextWriterTag.Legend);
					writer.Write("Original ASP.NET error page");
					writer.RenderEndTag(); // </legend>
					writer.Write(error.WebHostHtmlMessage);
					writer.RenderEndTag(); // </fieldset>

					//writer.RenderEndTag(); // </li>
				}


				//
				// Add a link to the source XML and JSON data.
				//
				/****** Taken out for now.
							writer.RenderBeginTag(HtmlTextWriterTag.Li);
							writer.Write("Raw/Source data in ");
           
							writer.AddAttribute(HtmlTextWriterAttribute.Href, "xml" + Request.Url.Query);
				#if NET_1_0 || NET_1_1
							writer.AddAttribute("rel", HtmlLinkType.Alternate);
				#else
							writer.AddAttribute(HtmlTextWriterAttribute.Rel, "alternate");
				#endif
							writer.AddAttribute(HtmlTextWriterAttribute.Type, "application/xml");
							writer.RenderBeginTag(HtmlTextWriterTag.A);
							writer.Write("XML");
							writer.RenderEndTag(); // </a>
							writer.Write(" or in ");

							writer.AddAttribute(HtmlTextWriterAttribute.Href, "json" + Request.Url.Query);
				#if NET_1_0 || NET_1_1
							writer.AddAttribute("rel", HtmlLinkType.Alternate);
				#else
							writer.AddAttribute(HtmlTextWriterAttribute.Rel, "alternate");
				#endif
							writer.AddAttribute(HtmlTextWriterAttribute.Type, "application/json");
							writer.RenderBeginTag(HtmlTextWriterTag.A);
							writer.Write("JSON");
							writer.RenderEndTag(); // </a>
           
							writer.RenderEndTag(); // </li>
				*/
				//
				// End of alternate links.
				//

				//writer.RenderEndTag(); // </ul>

				//
				// If this error has context, then write it out.
				// ServerVariables are good enough for most purposes, so
				// we only write those out at this time.
				//

				RenderCollection(writer, error.ServerVariables,
					"ServerVariables", "Server Variables");

				//base.Render(writer);
			}

			private void RenderCollection(HtmlTextWriter writer,
				NameValueCollection collection, string id, string title)
			{
				//            Debug.Assert(writer != null);
				//            Debug.AssertStringNotEmpty(id);
				//            Debug.AssertStringNotEmpty(title);

				//
				// If the collection isn't there or it's empty, then bail out.
				//

				if (collection == null || collection.Count == 0)
					return;

				//
				// Surround the entire section with a <div> element.
				//

				writer.AddAttribute(HtmlTextWriterAttribute.Id, id);
				writer.RenderBeginTag(HtmlTextWriterTag.Div);

				//
				// Write out the table caption.
				//

				writer.AddAttribute(HtmlTextWriterAttribute.Class, "table-caption");
				writer.RenderBeginTag(HtmlTextWriterTag.P);
				this.Server.HtmlEncode(title, writer);
				writer.RenderEndTag(); // </p>
				writer.WriteLine();

				//
				// Some values can be large and add scroll bars to the page
				// as well as ruin some formatting. So we encapsulate the
				// table into a scrollable view that is controlled via the
				// style sheet.
				//

				writer.AddAttribute(HtmlTextWriterAttribute.Class, "scroll-view");
				writer.RenderBeginTag(HtmlTextWriterTag.Div);

				//
				// Create a table to display the name/value pairs of the
				// collection in 2 columns.
				//

				Table table = new Table();
				table.CellSpacing = 0;

				//
				// Create the header row and columns.
				//

				TableRow headRow = new TableRow();

				TableHeaderCell headCell;

				headCell = new TableHeaderCell();
				headCell.Wrap = false;
				headCell.Text = "Name";
				headCell.CssClass = "name-col";

				headRow.Cells.Add(headCell);

				headCell = new TableHeaderCell();
				headCell.Wrap = false;
				headCell.Text = "Value";
				headCell.CssClass = "value-col";

				headRow.Cells.Add(headCell);

				table.Rows.Add(headRow);

				//
				// Create a row for each entry in the collection.
				//

				string[] keys = collection.AllKeys;
				Array.Sort(keys);

				for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
				{
					string key = keys[keyIndex];

					TableRow bodyRow = new TableRow();
					bodyRow.CssClass = keyIndex % 2 == 0 ? "even-row" : "odd-row";

					TableCell cell;

					//
					// Create the key column.
					//

					cell = new TableCell();
					cell.Text = Server.HtmlEncode(key);
					cell.CssClass = "key-col";

					bodyRow.Cells.Add(cell);

					//
					// Create the value column.
					//

					cell = new TableCell();
					cell.Text = Server.HtmlEncode(collection[key]);
					cell.CssClass = "value-col";

					bodyRow.Cells.Add(cell);

					table.Rows.Add(bodyRow);
				}

				//
				// Write out the table and close container tags.
				//

				table.RenderControl(writer);

				writer.RenderEndTag(); // </div>
				writer.WriteLine();

				writer.RenderEndTag(); // </div>
				writer.WriteLine();
			}
		}
	}
