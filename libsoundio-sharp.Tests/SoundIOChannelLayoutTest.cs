﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LibSoundIOSharp.Tests
{
	[TestFixture]
	public class SoundIOChannelLayoutTest
	{
		[Test]
		public void ChannelLayouts ()
		{
			Assert.AreNotEqual (0, SoundIOChannelLayout.BuiltInCount, "no built in channel layout?");
			for (int i = 0; i < SoundIOChannelLayout.BuiltInCount; i++) {
				var l = SoundIOChannelLayout.GetBuiltIn (i);
				var name = l.DetectBuiltInName ();
				Assert.AreNotEqual (null, name, "It should be built-in...");
				foreach (var c in l.Channels.Take (l.ChannelCount))
					Assert.AreNotEqual (SoundIOChannelId.Invalid, c, $"Until l.ChannelCount = {l.ChannelCount}, Channel ID should be valid");
				foreach (var c in l.Channels.Skip (l.ChannelCount))
					Assert.AreEqual (SoundIOChannelId.Invalid, c, $"After l.ChannelCount = {l.ChannelCount}, Channel ID should be invalid");
			}
			Assert.IsNull (SoundIOChannelLayout.GetDefault (0), "soundio returned non-null layout for zero-channels??");
			for (int channels = 1; channels < 10; channels++) {
				var l = SoundIOChannelLayout.GetDefault (channels);
				if (l != null) // some channels would give null e.g. there is no 9ch audio...
					Assert.IsNotNull (l.DetectBuiltInName (), $"channel layout for {channels} has no builtin name...");
			}
		}
	}
}
