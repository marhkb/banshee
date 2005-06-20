/***************************************************************************
 *  SearchEntry.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
using System;
using System.Collections;
using Gtk;

namespace Sonance
{
	public class SearchEntry : Frame
	{
		private ArrayList searchFields;
		private Hashtable menuMap;
		private Menu popupMenu;
		
		private HBox box;
		private Entry entry;
		private EventBox evBox;
		private Image img;
		private Tooltips tooltips;
	
		private Gdk.Pixbuf icon;
		private Gdk.Pixbuf hoverIcon;
		private Gdk.Cursor handCursor;
		
		private CheckMenuItem activeItem;
		private bool menuActive;
		
		public event EventHandler RunQuery;
	
		static GLib.GType gtype;
		public static new GLib.GType GType
		{
			get {
				if(gtype == GLib.GType.Invalid)
					gtype = RegisterGType(typeof(SearchEntry));
				return gtype;
			}
		}
	
		public SearchEntry(ArrayList searchFields) : base()
		{
			handCursor = new Gdk.Cursor(Gdk.CursorType.Hand1);
			tooltips = new Tooltips();
			tooltips.Enable();
			
			this.searchFields = searchFields;
			
			BuildWidget();
			BuildMenu();
		}
		
		private void BuildWidget()
		{
			box = new HBox();
			box.Show();
			Add(box);
			
			entry = new Entry();
			entry.HasFrame = false;
			entry.WidthChars = 15;
			entry.Activated += OnEntryActivated;
			entry.Show();
			
			icon = Gdk.Pixbuf.LoadFromResource("search-entry-icon.png");
			hoverIcon = Gdk.Pixbuf.LoadFromResource(
				"search-entry-icon-hover.png");
			
			img = new Image(icon);
			img.Show();
			//img.CanFocus = true;
			img.Xpad = 5;
			img.Ypad = 1;
			
			evBox = new EventBox();
			evBox.CanFocus = true;
			evBox.EnterNotifyEvent += OnEnterNotifyEvent;
			evBox.LeaveNotifyEvent += OnLeaveNotifyEvent;
			evBox.ButtonPressEvent += OnButtonPressEvent;
			evBox.KeyPressEvent += OnKeyPressEvent;
			evBox.FocusInEvent += OnFocusInEvent;
			evBox.FocusOutEvent += OnFocusOutEvent;
			evBox.Show();
			evBox.Add(img);
			evBox.ModifyBg(StateType.Normal, 
				entry.Style.Base(StateType.Normal));
			
			box.PackStart(evBox, false, false, 0);
			box.PackStart(entry, true, true, 0);
		}
		
		private void BuildMenu()
		{
			popupMenu = new Menu();
			menuMap = new Hashtable();
			
			popupMenu.Deactivated += OnMenuDeactivated;
			
			foreach(string menuLabel in searchFields) {
				if(menuLabel.Equals("-"))
					popupMenu.Append(new SeparatorMenuItem());
				else {
					CheckMenuItem item = new CheckMenuItem(menuLabel);
					item.DrawAsRadio = true;
					item.Toggled += OnMenuItemToggled;
					popupMenu.Append(item);
					
					if(activeItem == null) {
						activeItem = item;
						item.Active = true;
					}
					
					menuMap[item] = menuLabel;
				}
			}	
			
			tooltips.SetTip(evBox, "Searching: " + Field, null);
		}
		
		private void ShowMenu(uint time)
		{
			popupMenu.Popup(null, null, new MenuPositionFunc(MenuPosition), 
				IntPtr.Zero, 0, time);
			popupMenu.ShowAll();
			img.Pixbuf = hoverIcon;
			menuActive = true;
		}
		
		private void OnEnterNotifyEvent(object o, EnterNotifyEventArgs args)
		{
			img.GdkWindow.Cursor = handCursor;
			img.Pixbuf = hoverIcon;
		}
		
		private void OnLeaveNotifyEvent(object o, LeaveNotifyEventArgs args)
		{
			if(!evBox.HasFocus && !menuActive)
				img.Pixbuf = icon;
		}
		
		private void OnButtonPressEvent(object o, ButtonPressEventArgs args)
		{
			ShowMenu(args.Event.Time);
		}
		
		private void OnFocusInEvent(object o, EventArgs args)
		{
			img.Pixbuf = hoverIcon;
		}
		
		private void OnFocusOutEvent(object o, EventArgs args)
		{
			if(!menuActive)
				img.Pixbuf = icon;
		}
		
		private void OnKeyPressEvent(object o, KeyPressEventArgs args)
		{
			if(args.Event.Key != Gdk.Key.Return)
				return;
				
			ShowMenu(args.Event.Time);
		}

		private void MenuPosition(Menu menu, out int x, out int y, 
			out bool push_in)
		{
			int pX, pY;
			
			GdkWindow.GetOrigin(out pX, out pY);
			Requisition req = SizeRequest();
			
			x = pX + Allocation.X;
			y = pY + Allocation.Y + req.Height;	
			push_in = true;
		}
		
		private void OnMenuItemToggled(object o, EventArgs args)
		{
			CheckMenuItem item = o as CheckMenuItem;
			if(activeItem == item)
				item.Active = true;
			else
				activeItem = item;
				
			foreach(MenuItem iterItem in popupMenu.Children) {
				if(!(iterItem is CheckMenuItem))
					continue;
				
				CheckMenuItem checkItem = iterItem as CheckMenuItem;
					
				if(checkItem != activeItem)
					checkItem.Active = false;
			}
			
			activeItem.Active = true;
			
			tooltips.SetTip(evBox, "Searching: " + Field, null);
			
			entry.HasFocus = true;
		}
		
		private void OnMenuDeactivated(object o, EventArgs args)
		{
			img.Pixbuf = icon;
			menuActive = false;
		}
		
		private void OnEntryActivated(object o, EventArgs args)
		{
			EventHandler handler = RunQuery;
			if(handler != null)
				handler(this, new EventArgs());
		}
		
		public string Query
		{
			get {
				return entry.Text;
			}
			
			set {
				entry.Text = value;
			}
		}
		
		public string Field
		{
			get {
				return menuMap[activeItem] as string;
			}
		}
	}
}
