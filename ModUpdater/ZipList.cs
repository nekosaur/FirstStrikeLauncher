using System;
using System.IO;

namespace BF2Utils
{
	public enum CallType
	{
		Add,
		Remove
	}
	
	public class ZipCall
	{
		public string ZipPath
		{
			get { return _zipPath; }
			set { _zipPath = value; }
		}
		private string _zipPath;
		
		public CallType Type
		{
			get { return _type; }
			set { _type = value; }
		}
		private CallType _type;
		
		public ZipCall(string zipPath, CallType callType)
		{
			_zipPath = zipPath;
			_type = callType;
		}
	}
	
	public class ZipInfo
	{
		public string FolderPath
		{
			get { return _folderPath; }
			set { _folderPath = value; }
		}
		private string _folderPath;
		
		public string FileSuffix
		{
			get { return _fileSuffix; }
			set { _fileSuffix = value; }
		}
		private string _fileSuffix;
		
		public string FullPath
		{
			get { return _folderPath + _fileSuffix; }
		}
		
		public ZipInfo(string folderPath, string fileSuffix)
		{
			_folderPath = folderPath.ToLower();
			_fileSuffix = fileSuffix.ToLower();
		}
		
		public override string ToString()
		{
			return FullPath;
		}
		
		public override bool Equals(object obj)
		{
			return obj.ToString().Equals(ToString());
		}
		
		public override int GetHashCode()
		{
			return ToString().GetHashCode();
		}
		
		
	}
	
	public class ZipList
	{
		
		public ZipList ()
		{
		}
	}
}

