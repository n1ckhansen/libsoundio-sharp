﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LibSoundIOSharp
{
	public class SoundIO : IDisposable
	{
		Pointer<SoundIo> handle;

		public SoundIO ()
		{
			handle = Natives.soundio_create ();
		}

		public void Dispose ()
		{
			foreach (var h in allocated_hglobals)
				Marshal.FreeHGlobal (h);
			Natives.soundio_destroy (handle);
		}

		SoundIo GetValue ()
		{
			return Marshal.PtrToStructure<SoundIo> (handle);
		}

		// fields

		// FIXME: this should be taken care in more centralized/decent manner... we don't want to write
		// this kind of code anywhere we need string marshaling.
		List<IntPtr> allocated_hglobals = new List<IntPtr> ();

		public string ApplicationName {
			get { return Marshal.PtrToStringAnsi (GetValue ().app_name); }
			set {
				unsafe {
					var existing = GetValue ().app_name;
					if (allocated_hglobals.Contains (existing)) {
						allocated_hglobals.Remove (existing);
						Marshal.FreeHGlobal (existing);
					}
					var ptr = Marshal.StringToHGlobalAnsi (value);
					Marshal.WriteIntPtr (handle, app_name_offset, ptr);
					allocated_hglobals.Add (ptr);
				}
			}
		}
		static readonly int app_name_offset = (int)Marshal.OffsetOf<SoundIo> ("app_name");

		public SoundIOBackend CurrentBackend {
			// read only.
			get { return (SoundIOBackend) GetValue ().current_backend; }
		}

		// emit_rtprio_warning
		public Action EmitRealtimePriorityWarning {
			get { return emit_rtprio_warning; }
			set {
				emit_rtprio_warning = value;
				var ptr = Marshal.GetFunctionPointerForDelegate (on_devices_change);
				Marshal.WriteIntPtr (handle, emit_rtprio_warning_offset, ptr);
			}
		}
		static readonly int emit_rtprio_warning_offset = (int)Marshal.OffsetOf<SoundIo> ("emit_rtprio_warning");
		Action emit_rtprio_warning;

		// jack_error_callback
		public Action<string> JackErrorCallback {
			get { return jack_error_callback; }
			set {
				jack_error_callback = value;
				var ptr = Marshal.GetFunctionPointerForDelegate (jack_error_callback);
				Marshal.WriteIntPtr (handle, jack_error_callback_offset, ptr);
			}
		}
		static readonly int jack_error_callback_offset = (int)Marshal.OffsetOf<SoundIo> ("jack_error_callback");
		Action<string> jack_error_callback;

		// jack_info_callback
		public Action<string> JackInfoCallback {
			get { return jack_info_callback; }
			set {
				jack_info_callback = value;
				var ptr = Marshal.GetFunctionPointerForDelegate (jack_info_callback);
				Marshal.WriteIntPtr (handle, jack_info_callback_offset, ptr);
			}
		}
		static readonly int jack_info_callback_offset = (int)Marshal.OffsetOf<SoundIo> ("jack_info_callback");
		Action<string> jack_info_callback;

		// on_backend_disconnect
		public Action<SoundIO,int> OnBackendDisconnect {
			get { return on_backend_disconnect; }
			set {
				on_backend_disconnect = value;
				var ptr = Marshal.GetFunctionPointerForDelegate (on_backend_disconnect);
				Marshal.WriteIntPtr (handle, on_backend_disconnect_offset, ptr);
			}
		}
		static readonly int on_backend_disconnect_offset = (int)Marshal.OffsetOf<SoundIo> ("on_backend_disconnect");
		Action<SoundIO,int> on_backend_disconnect;

		// on_devices_change
		public Action<SoundIO> OnDevicesChange {
			get { return on_devices_change; }
			set {
				on_devices_change = value;
				var ptr = Marshal.GetFunctionPointerForDelegate (on_devices_change);
				Marshal.WriteIntPtr (handle, on_devices_change_offset, ptr);
			}
		}
		static readonly int on_devices_change_offset = (int)Marshal.OffsetOf<SoundIo> ("on_devices_change");
		Action<SoundIO> on_devices_change;

		// on_events_signal
		public Action<SoundIO> OnEventsSignal {
			get { return on_events_signal; }
			set {
				on_events_signal = value;
				var ptr = Marshal.GetFunctionPointerForDelegate (on_events_signal);
				Marshal.WriteIntPtr (handle, on_events_signal_offset, ptr);
			}
		}
		static readonly int on_events_signal_offset = (int)Marshal.OffsetOf<SoundIo> ("on_events_signal");
		Action<SoundIO> on_events_signal;


		// functions

		public int BackendCount {
			get { return Natives.soundio_backend_count (handle); }
		}

		public int InputDeviceCount {
			get { return Natives.soundio_input_device_count (handle); }
		}

		public int OutputDeviceCount {
			get { return Natives.soundio_output_device_count (handle); }
		}

		public int DefaultInputDeviceIndex {
			get { return Natives.soundio_default_input_device_index (handle); }
		}

		public int DefaultOutputDeviceIndex {
			get { return Natives.soundio_default_output_device_index (handle); }
		}

		public SoundIOBackend GetBackend (int index)
		{
			return (SoundIOBackend) Natives.soundio_get_backend (handle, index);
		}

		public SoundIODevice GetInputDevice (int index)
		{
			return new SoundIODevice (Natives.soundio_get_input_device (handle, index));
		}

		public SoundIODevice GetOutputDevice (int index)
		{
			return new SoundIODevice (Natives.soundio_get_output_device (handle, index));
		}

		public void Connect ()
		{
			var ret = (SoundIoError) Natives.soundio_connect (handle);
			if (ret != SoundIoError.SoundIoErrorNone)
				throw new SoundIOException (ret);
		}

		public void ConnectBackend (SoundIOBackend backend)
		{
			var ret = (SoundIoError) Natives.soundio_connect_backend (handle, (SoundIoBackend) backend);
			if (ret != SoundIoError.SoundIoErrorNone)
				throw new SoundIOException (ret);
		}

		public void Disconnect ()
		{
			Natives.soundio_disconnect (handle);
		}

		public void FlushEvents ()
		{
			Natives.soundio_flush_events (handle);
		}

		public void WaitEvents ()
		{
			Natives.soundio_wait_events (handle);
		}

		public void Wakeup ()
		{
			Natives.soundio_wakeup (handle);
		}

		public void ForceDeviceScan ()
		{
			Natives.soundio_force_device_scan (handle);
		}

		public SoundIORingBuffer CreateRingBuffer (int capacity)
		{
			return new SoundIORingBuffer (Natives.soundio_ring_buffer_create (handle, capacity));
		}

		// static methods

		public static string VersionString {
			get { return Marshal.PtrToStringAnsi (Natives.soundio_version_string ()); }
		}

		public static int VersionMajor {
			get { return Natives.soundio_version_major (); }
		}

		public static int VersionMinor {
			get { return Natives.soundio_version_minor (); }
		}

		public static int VersionPatch {
			get { return Natives.soundio_version_patch (); }
		}

		public static string GetBackendName (SoundIOBackend backend)
		{
			return Marshal.PtrToStringAnsi (Natives.soundio_backend_name ((SoundIoBackend) backend));
		}

		public static bool HaveBackend (SoundIOBackend backend)
		{
			return Natives.soundio_have_backend ((SoundIoBackend) backend) != 0;
		}

		public static int GetBytesPerSample (SoundIOFormat format)
		{
			return Natives.soundio_get_bytes_per_sample ((SoundIoFormat) format);
		}

		public static int GetBytesPerFrame (SoundIOFormat format, int channelCount)
		{
			return Natives.soundio_get_bytes_per_frame ((SoundIoFormat) format, channelCount);
		}

		public static int GetBytesPerSecond (SoundIOFormat format, int channelCount, int sampleRate)
		{
			return Natives.soundio_get_bytes_per_second ((SoundIoFormat) format, channelCount, sampleRate);
		}

		public static string GetSoundFormatName (SoundIOFormat format)
		{
			return Marshal.PtrToStringAnsi (Natives.soundio_format_string ((SoundIoFormat) format));
		}
	}
}