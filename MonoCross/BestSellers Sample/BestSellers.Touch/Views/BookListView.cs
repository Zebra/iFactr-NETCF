using System;
using System.Collections.Generic;
using System.Linq;

using UIKit;
using MonoTouch.Dialog;

using MonoCross.Navigation;
using MonoCross.Touch;

using Touch.TestContainer;
using BestSellers;

namespace Touch.TestContainer.Views
{
	[MXTouchViewAttributes(ViewNavigationContext.Master)]
	public class BookListView : MXTouchDialogView<BookList>
	{
		public BookListView(): base(UITableViewStyle.Plain, null, true)
		{
		}
		
		public override void Render ()
		{
			RootElement root = new RootElement(Model.CategoryDisplayName ?? Model.Category);
			Section section = new Section();
			
			foreach (var book in Model)
			{
				string isbn = string.IsNullOrEmpty(book.ISBN10) ? book.ISBN13 : book.ISBN10;
				string uri = String.Format("{0}/{1}", book.CategoryEncoded, isbn);
			 	StringElement se = new StringElement(book.Title, () => {  this.Navigate(uri); });
				section.Add(se);
			}
			
			root.Add(section);
			Root = root;
		}
	}
}

