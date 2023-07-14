using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "28a43c469f9fe6ed9ab51b68edb4c7a7d578a0da")]
public class CRunConsoleCommand : CEventHandler
{
	public string console_command;

	public override void Activate(Component sender)
	{
		Unigine.Console.Run(console_command);
	}
}