using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

namespace TaskMan.Objects
{
	/// <summary>
	/// Specifies the line breaking rule if
	/// the text to be written into a field
	/// exceeds the field length.
	/// </summary>
	public enum LineBreaking
	{
		/// <summary>
		/// Allows line breaking inside the word.
		/// </summary>
		None,
		/// <summary>
		/// Preferably breaks on whitespace,
		/// but can break inside a word if it 
		/// is longer than the field width.
		/// </summary>
		Whitespace,
	}

	/// <summary>
	/// Specifies the text alignment inside
	/// a field.
	/// </summary>
	public enum Align
	{
		/// <summary>
		/// Aligns the text left inside the
		/// field.
		/// </summary>
		Left,
		/// <summary>
		/// Aligns the text right inside the
		/// field.
		/// </summary>
		Right,
		/// <summary>
		/// Aligns the text centrally inside
		/// the field.
		/// </summary>
		Center,
	}

	public class TableWriter : IDisposable
	{
		FieldRule[] _fieldRules;
		TextWriter _output;

		public struct FieldRule
		{
			public int Width { get; }
			public LineBreaking LineBreaking { get; }
			public Align Align { get; }

			public FieldRule(
				int width = 10,
				LineBreaking lineBreaking = LineBreaking.None,
				Align align = Align.Left)
			{
				this.Width = width;
				this.LineBreaking = lineBreaking;
				this.Align = align;
			}
		}

		/// <summary>
		/// Breaks the specified field value into lines not 
		/// exceeding the field width, and aligns the text 
		/// on each line according to the specified alignment rules.
		/// </summary>
		private static Queue<string> MakeFieldLines(
			string text,
			FieldRule fieldRule)
		{
			List<string> resultingLines = new List<string>();
			
			if (fieldRule.LineBreaking == LineBreaking.None)
			{
				resultingLines = text
					.Split(fieldRule.Width)
					.Select(characters => new string(characters.ToArray()))
					.ToList();
			}
			else if (fieldRule.LineBreaking == LineBreaking.Whitespace)
			{
				Queue<string> textParts = new Queue<string>(Regex
					.Split(@"(\s)", text)
                    .SelectMany(part =>
					{
						if (part.Length <= fieldRule.Width)
						{
							return new[] { part };
						}
						else 
						{
							return part
								.Split(fieldRule.Width)
								.Select(characters => new string(characters.ToArray()))
								.ToArray();
						}
					}));

				string nextLine = string.Empty;

				while (textParts.Count > 0)
				{
					string linePart = textParts.Dequeue();

					if (nextLine.Length + linePart.Length > fieldRule.Width)
					{
						resultingLines.Add(nextLine.Trim());
						nextLine = string.Empty;
					}

					nextLine += linePart;
				}

				if (!string.IsNullOrEmpty(nextLine))
				{
					resultingLines.Add(nextLine.Trim());
				}
			}

			return new Queue<string>(resultingLines.Select(line =>
			{
				if (fieldRule.Align == Align.Left)
				{
					return line.PadRight(fieldRule.Width);
				}
				else if (fieldRule.Align == Align.Right)
				{
					return line.PadLeft(fieldRule.Width);
				}
				else if (fieldRule.Align == Align.Center)
				{
					throw new NotImplementedException();
				}
				else
				{
					throw new Exception();
				}
			}));
		}

		public TableWriter(TextWriter output, params FieldRule[] fieldRules)
		{
			if (output == null) throw new ArgumentNullException(nameof(output));
			if (fieldRules == null) throw new ArgumentNullException(nameof(fieldRules));

			this._output = output;
			this._fieldRules = fieldRules;
		}

		/// <summary>
		/// Returns the total width of all table fields.
		/// </summary>
		public int TotalWidth => _fieldRules.Aggregate(
			0, 
			(accumulator, fieldRule) => accumulator += fieldRule.Width);

		/// <summary>
		/// Writes a single table row using the specified
		/// field values.
		/// </summary>
		public void WriteLine(params object[] fieldValues)
		{
			if (fieldValues.Length != _fieldRules.Length)
			{
				throw new ArgumentException();
			}

			Queue<string>[] rowLines = new Queue<string>[fieldValues.Length];

			for (int fieldIndex = 0; fieldIndex < fieldValues.Length; ++fieldIndex)
			{
				rowLines[fieldIndex] = MakeFieldLines(
					fieldValues[fieldIndex].ToString(),
					_fieldRules[fieldIndex]);
			}

			// Write the current table row.
			// Process continues while there is still
			// at least one field with unwritten lines.
			// -
			while (rowLines.Any(fieldLines => fieldLines.Count > 0))
			{
				rowLines.ForEach((fieldLineCollection, fieldIndex) =>
				{
					if (fieldLineCollection.Any())
					{
						_output.Write(fieldLineCollection.Dequeue());
					}
					else
					{
						// Write an empty placeholder for this field
						// because its line collection is already empty.
						// -
						_output.Write(
							Enumerable.Repeat(" ", _fieldRules[fieldIndex].Width));
					}
				});

				_output.WriteLine();
			}
		}

		/// <summary>
		/// Closes the output writer and disposes of the
		/// current object.
		/// </summary>
		public void Close()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void IDisposable.Dispose()
		{
			Close();
		}

		protected virtual void Dispose(bool isDisposing)
		{
			if (isDisposing)
			{
				_output.Dispose();
			}
		}
	}
}

