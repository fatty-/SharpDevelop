﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Martin Konicek" email="martin.konicek@gmail.com"/>
//     <version>$Revision: $</version>
// </file>
using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace ICSharpCode.SharpDevelop.Refactoring
{
	/// <summary>
	/// Description of ContextActionsPopup.
	/// </summary>
	public class ContextActionsPopup : Popup
	{
		public ContextActionsPopup()
		{
			this.StaysOpen = false;	// Close on lost focus
			this.ActionsControl = new ContextActionsControl();
			// Close when any action excecuted
			this.ActionsControl.ActionExecuted += delegate { this.Close(); };
		}
		
		private ContextActionsControl ActionsControl 
		{
			get { return (ContextActionsControl)this.Child; }
			set { this.Child = value; }
		}
		
		public ContextActionsViewModel Actions 
		{
			get { return (ContextActionsViewModel)ActionsControl.DataContext; }
			set { ActionsControl.DataContext = value; }
		}
		
		public new void Focus()
		{
			this.ActionsControl.Focus();
		}
		
		public void Open()
		{
			this.IsOpen = true;
		}
		
		public void Close()
		{
			this.IsOpen = false;
		}
	}
}
