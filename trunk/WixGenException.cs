using System;
using System.Globalization;

//using System.Runtime.Serialization;
// using System.Security.Permissions;

//using NAnt.Core.Util;

namespace Sitronics.Installer
{
	/// <summary>
	/// Thrown whenever an error occurs during the build.
	/// </summary>
	public class WixGenException : ApplicationException
	{
		#region Private Instance Fields

		#endregion Private Instance Fields

		#region Public Instance Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="WixGenException" /> class.
		/// </summary>
		public WixGenException() 
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WixGenException" /> class 
		/// with a descriptive message.
		/// </summary>
		/// <param name="message">A descriptive message to include with the exception.</param>
		public WixGenException(String message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WixGenException" /> class
		/// with the specified descriptive message and inner exception.
		/// </summary>
		/// <param name="message">A descriptive message to include with the exception.</param>
		/// <param name="innerException">A nested exception that is the cause of the current exception.</param>
		public WixGenException(String message, Exception innerException) : base(message, innerException)
		{
		}

		#endregion Public Instance Constructors

		#region Public Instance Properties

		/// <summary>
		/// Gets the raw message as specified when the exception was 
		/// constructed.
		/// </summary>
		/// <value>
		/// The raw message as specified when the exception was 
		/// constructed.
		/// </value>
		public string RawMessage
		{
			get { return base.Message; }
		}

		#endregion Public Instance Properties

		#region Override implementation of Object

		/// <summary>
		/// Creates and returns a string representation of the current 
		/// exception.
		/// </summary>
		/// <returns>
		/// A string representation of the current exception.
		/// </returns>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}:{1}{2}",
			                     Message, Environment.NewLine, base.ToString());
		}

		#endregion Override implementation of Object
	}
}