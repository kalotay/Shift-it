﻿using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using ShiftIt.Ftp;

namespace ShiftIt.Integration.Tests
{
	[SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
	[TestFixture]
	public class FtpSessionTests
	{
		IFtpSession _subject;

		[SetUp]
		public void setup()
		{
			_subject = new FtpSession();
		}

		[TearDown]
		public void teardown()
		{
			_subject.Dispose();
		}
		
		[Test]
		public void should_be_able_to_connect_to_a_public_ftp_server_and_list_files ()
		{
			_subject.SetRemoteHost("ftp.mozilla.org");
			_subject.SetRemotePath("pub");
			_subject.ListMode = DirectoryListMode.NameList;
			var list = _subject.GetFileList("*");

			Assert.That(list, Contains.Item("README"));
		}
	}
}