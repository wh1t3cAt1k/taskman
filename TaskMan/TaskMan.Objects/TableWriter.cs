using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

	public enum TableBorders
	{
		None = 0,
		OuterLeft = 1,
		OuterRight = 1 << 1,
		OuterTop = 1 << 2,
		OuterBottom = 1 << 3,
		OuterVertical = OuterLeft | OuterRight,
		OuterHorizontal = OuterTop | OuterBottom,
		Outer = OuterVertical | OuterHorizontal,
		InnerVertical = 1 << 4,
		InnerHorizontal = 1 << 5,
		Inner = InnerVertical | InnerHorizontal,
		All = Outer | Inner,
	}

	public class TableWriter : IDisposable
	{
		FieldRule[] _fieldRules;
		TableBorders _tableBorders;
		TextWriter _output;

		const string TOP_LEFT_JOINT = "┌";
		const string TOP_RIGHT_JOINT = "┐";
		const string BOTTOM_LEFT_JOINT = "└";
		const string BOTTOM_RIGHT_JOINT = "┘";
		const string TOP_JOINT = "┬";
		const string BOTTOM_JOINT = "┴";
		const string LEFT_JOINT = "├";
		const string JOINT = "┼";
		const string RIGHT_JOINT = "┤";
		const string HORIZONTAL_LINE = "─";
		const string VERTICAL_LINE = "│";

		private int LineHeight => 
			_fieldRules.Max(fieldRule => fieldRule.PaddingTop + fieldRule.PaddingBottom);

		public struct FieldRule
		{
			/// <summary>
			/// Gets the field width.
			/// </summary>
			public int Width { get; }

			/// <summary>
			/// Gets the amount of left padding, in spaces.
			/// </summary>
			public int PaddingLeft { get; }

			/// <summary>
			/// Gets the amount of right padding, in spaces.
			/// </summary>
			public int PaddingRight { get; }

			/// <summary>
			/// Gets the full width of the field,
			/// accounting for any left and right padding.
			/// </summary>
			public int FullWidth => Width + PaddingLeft + PaddingRight;

			/// <summary>
			/// Gets the amount of top padding, in lines.
			/// </summary>
			public int PaddingTop { get; }

			/// <summary>
			/// Gets the amount of bottom padding, in lines.
			/// </summary>
			/// <value>The padding bottom.</value>
			public int PaddingBottom { get; }

			/// <summary>
			/// Gets the line breaking rules.
			/// </summary>
			public LineBreaking LineBreaking { get; }

			/// <summary>
			/// Gets the text alignment rules.
			/// </summary>
			public Align Align { get; }

			public FieldRule(
				int width = 10,
				LineBreaking lineBreaking = LineBreaking.None,
				Align align = Align.Left,
				int paddingLeft = 0,
				int paddingRight = 0,
				int paddingTop = 0,
				int paddingBottom = 0)
			{
				if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));

				if (paddingLeft < 0) throw new ArgumentOutOfRangeException(nameof(paddingLeft));
				if (paddingRight < 0) throw new ArgumentOutOfRangeException(nameof(paddingRight));
				if (paddingTop < 0) throw new ArgumentOutOfRangeException(nameof(paddingTop));
				if (paddingBottom < 0) throw new ArgumentOutOfRangeException(nameof(paddingBottom));

				this.Width = width;
				this.Align = align;
				this.LineBreaking = lineBreaking;

				this.PaddingLeft = paddingLeft;
				this.PaddingRight = paddingRight;
				this.PaddingTop = paddingTop;
				this.PaddingBottom = paddingBottom;
			}
		}

		/// <summary>
		/// Gets an empty line with whose width
		/// is equal to the provided field rule's
		/// width (excluding any left or right padding).
		/// </summary>
		private static string EmptyLine(FieldRule fieldRule)
		{
			return " ".Repeat(fieldRule.Width);
		}

		/// <summary>
		/// Breaks the specified field value into lines that
		/// do not exceeding the field width, and aligns the text 
		/// on each line according to the specified alignment rules.
		/// </summary>
		private static Queue<string> MakeFieldLines(
			string text,
			FieldRule fieldRule)
		{
			List<string> resultingLines = new List<string>();

			// Account for top padding
			// -
			resultingLines.AddRange(
				Enumerable.Repeat(EmptyLine(fieldRule), fieldRule.PaddingTop));

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
					.Split(text, @"(\s)")
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

			// Account for bottom padding
			// -
			resultingLines.AddRange(
				Enumerable.Repeat(EmptyLine(fieldRule), fieldRule.PaddingBottom));

			return new Queue<string>(resultingLines.Select(line =>
			{
				// Account for alignment
				// -
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
			}).Select(line =>
			{
				// Account for left and right padding
				// -
				return line
					.PadLeft(fieldRule.Width + fieldRule.PaddingLeft)
					.PadRight(fieldRule.FullWidth);
			}));
		}

		public TableWriter(
			TextWriter output, 
			TableBorders tableBorders, 
			params FieldRule[] fieldRules)
		{
			if (output == null) throw new ArgumentNullException(nameof(output));
			if (fieldRules == null) throw new ArgumentNullException(nameof(fieldRules));

			this._output = output;
			this._fieldRules = fieldRules;
			this._tableBorders = tableBorders;
		}

		/// <summary>
		/// Returns the total width of all table fields.
		/// </summary>
		public int TotalWidth => _fieldRules.Aggregate(
			0, 
			(accumulator, fieldRule) => accumulator += fieldRule.FullWidth);

		private void PrintHorizontalBorder(TableBorders borderType)
		{
			if (borderType != TableBorders.OuterTop &&
				borderType != TableBorders.InnerHorizontal &&
				borderType != TableBorders.OuterBottom)
			{
				throw new ArgumentException(nameof(borderType));
			}

			string leftJoint;
			string middleJoint;
			string rightJoint;

			bool hasInnerVerticalBorders = 
				(_tableBorders & TableBorders.InnerVertical) != 0;

			if ((_tableBorders & borderType & TableBorders.OuterTop) != 0)
			{
				leftJoint = TOP_LEFT_JOINT;

				middleJoint = hasInnerVerticalBorders ?
					TOP_JOINT :
					HORIZONTAL_LINE;

				rightJoint = TOP_RIGHT_JOINT;
			}
			else if ((_tableBorders & borderType & TableBorders.InnerHorizontal) != 0)
			{
				leftJoint = LEFT_JOINT;

				middleJoint = hasInnerVerticalBorders ?
					JOINT :
					HORIZONTAL_LINE;

				rightJoint = RIGHT_JOINT;
			}
			else if ((_tableBorders & borderType & TableBorders.OuterBottom) != 0)
			{
				leftJoint = BOTTOM_LEFT_JOINT;

				middleJoint = hasInnerVerticalBorders ?
					BOTTOM_JOINT :
					HORIZONTAL_LINE;

				rightJoint = BOTTOM_RIGHT_JOINT;
			}
			else
			{
				return;
			}

			_fieldRules.ForEach((fieldRule, isFirstColumn, isLastColumn) =>
			{
				_output.Write(isFirstColumn ? leftJoint : middleJoint);
				_output.Write(HORIZONTAL_LINE.Repeat(fieldRule.FullWidth));

				if (isLastColumn)
				{
					_output.Write(rightJoint);
				}
			});

			_output.WriteLine();
		}

		private void PrintVerticalBorder(TableBorders borderType)
		{
			if (borderType != TableBorders.OuterLeft &&
			    borderType != TableBorders.InnerVertical &&
			    borderType != TableBorders.OuterRight)
			{
				throw new ArgumentException(nameof(borderType));
			}

			if ((_tableBorders & borderType & TableBorders.OuterLeft) != 0 ||
			    (_tableBorders & borderType & TableBorders.InnerVertical) != 0 ||
			    (_tableBorders & borderType & TableBorders.OuterRight) != 0)
			{
				_output.Write(VERTICAL_LINE);
			}
			else
			{
				return;
			}
		}

		/// <summary>
		/// Writes a single table row using the specified
		/// field values.
		/// </summary>
		public void WriteLine(bool isFirstRow, bool isLastRow, params object[] fieldValues)
		{
			if (fieldValues.Length != _fieldRules.Length)
			{
				throw new ArgumentException();
			}

			Queue<string>[] rowLines = new Queue<string>[fieldValues.Length];

			for (int fieldIndex = 0; fieldIndex < fieldValues.Length; ++fieldIndex)
			{
				rowLines[fieldIndex] = MakeFieldLines(
					fieldValues[fieldIndex]?.ToString() ?? string.Empty,
					_fieldRules[fieldIndex]);
			}

			// Normalize the field line collection counts so
			// that they contain the same number of lines.
			// -
			int maximumLines = rowLines.Max(lineCollection => lineCollection.Count);

			rowLines.ForEach((lineCollection, fieldIndex) => 
			{
				while (lineCollection.Count < maximumLines)
				{
					lineCollection.Enqueue(
						" ".Repeat(_fieldRules[fieldIndex].FullWidth));
				}
			});

			PrintHorizontalBorder(
				isFirstRow ? TableBorders.OuterTop : TableBorders.InnerHorizontal);

			// Write the current table row.
			// Process continues while there is still
			// at least one field with unwritten lines.
			// -
			while (rowLines.Any(fieldLines => fieldLines.Any()))
			{
				rowLines.ForEach((fieldLineCollection, isFirstColumn, isLastColumn) =>
				{
					PrintVerticalBorder(
						isFirstColumn ? TableBorders.OuterLeft : TableBorders.InnerVertical);

					_output.Write(fieldLineCollection.Dequeue());

					if (isLastColumn)
					{
						PrintVerticalBorder(TableBorders.OuterRight);
					}
				});
					
				_output.WriteLine();
			}

			PrintHorizontalBorder(
				isLastRow ? TableBorders.OuterBottom : TableBorders.InnerHorizontal);
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

