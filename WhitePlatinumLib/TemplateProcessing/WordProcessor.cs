using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using Dw = DocumentFormat.OpenXml.Drawing;
using Dw10 = DocumentFormat.OpenXml.Office2010.Drawing;

// TODO: Refactor
namespace WhitePlatinumLib.TemplateProcessing {
	public class WordProcessor {
		DataSet Data;
		MainDocumentPart MainPart;
		Document Document;

		// TODO: Handle style differently
		RunProperties CurrentRun;

		// This number is arbitrary (?)
		uint FreeIDSlot = 1024;

		Scripting Scripting;

		public WordProcessor(DataSet Data) {
			this.Data = Data;
		}

		public byte[] Process(byte[] TemplateFile) {
			using (MemoryStream TemplateFileStream = new MemoryStream()) {
				TemplateFileStream.Write(TemplateFile, 0, TemplateFile.Length);

				Process(TemplateFileStream);

				TemplateFileStream.Seek(0, SeekOrigin.Begin);
				return TemplateFileStream.ToArray();
			}
		}

		public void Process(Stream TemplateFileStream) {
			OpenSettings Settings = new OpenSettings();
			Settings.AutoSave = true;
			//Settings.MarkupCompatibilityProcessSettings = new MarkupCompatibilityProcessSettings(MarkupCompatibilityProcessMode.ProcessAllParts, FileFormatVersions.Office2016);

			using (WordprocessingDocument TemplateDocument = WordprocessingDocument.Open(TemplateFileStream, true, Settings)) {
				MainPart = TemplateDocument.MainDocumentPart;
				Document = MainPart.Document;
				Body Body = Document.Body;

				Scripting = new Scripting();

				HeaderPart[] Headers = MainPart.HeaderParts.ToArray();
				for (int i = 0; i < Headers.Length; i++)
					Process(Body, Headers[i].Header);

				FooterPart[] Footers = MainPart.FooterParts.ToArray();
				for (int i = 0; i < Footers.Length; i++)
					Process(Body, Footers[i].Footer);

				OpenXmlElement[] Elements = Body.Elements().ToArray();
				for (int i = 0; i < Elements.Length; i++)
					Process(Body, Elements[i]);

				Process(Body, Body);
				TemplateDocument.Save();
			}
		}

		void Process(Body Body, OpenXmlElement Element) {
			string InnerText = Element.InnerText;

			if (VerifyCodeToken(ref InnerText)) {
				Element.Remove();
				Scripting.Eval(InnerText);
				return;
			}

			if (Scripting.CaptureElement(Body, Element)) {
				Element.Remove();
				return;
			}

			if (string.IsNullOrEmpty(InnerText))
				return;

			if (VerifyToken(ref InnerText, out TokenParameters TokenParams)) {
				DataType DataType = Data.GetObject(InnerText, out object Obj);

				if ((CurrentRun = Element.GetFirstChild<RunProperties>()) == null) {
					SdtProperties SdtProps = Element.GetFirstChild<SdtProperties>();

					if (SdtProps != null)
						CurrentRun = SdtProps.GetFirstChild<RunProperties>();
				}


				if (Element is SdtContentBlock SdtContentBlock) {
					OpenXmlElement NewContent = GenerateContent(Body, DataType, Obj, TokenParams);

					// TODO: Non existant template token, do something?
					if (NewContent == null)
						throw new NotImplementedException();

					if (NewContent is Run)
						NewContent = new Paragraph(new ParagraphProperties(), NewContent);

					if (DataType == DataType.PageBreak) {
						SdtContentBlock.Parent.Parent.ReplaceChild(NewContent, SdtContentBlock.Parent);
					} else {
						SdtContentBlock NewBlock = new SdtContentBlock();
						NewBlock.AppendChild(NewContent);

						SdtContentBlock.Parent.ReplaceChild(NewBlock, SdtContentBlock);
					}
					return;
				} else if (Element is SdtContentRun SdtContentRun) {
					// if (SdtRun) seems to replace this part?
					throw new NotImplementedException();
				} else if (Element is SdtRun SdtRun) {
					OpenXmlElement NewContent = GenerateContent(Body, DataType, Obj, TokenParams);

					// TODO: Non existant template token, do something?
					if (NewContent == null)
						throw new NotImplementedException();

					if (NewContent is Table && ParentsAreElement<Paragraph>(SdtRun, out OpenXmlElement FoundParent)) {
						FoundParent.Parent.ReplaceChild(NewContent, FoundParent);
						return;
					}

					Element.Parent.ReplaceChild(NewContent, SdtRun);
					return;
				}
			}

			// Array is used here because element children are dynamically changed, IEnumerable may have side effects
			OpenXmlElement[] Elements = Element.Elements().ToArray();
			for (int i = 0; i < Elements.Length; i++) {
				OpenXmlElement E = Elements[i];

				if (E is Table T)
					ProcessTable(Body, T);
				else
					Process(Body, E);
			}
		}

		void ProcessTable(Body Body, Table Table) {
			TableRow[] TableRows = Table.Elements<TableRow>().ToArray();
			TableProperties TableProps = Table.Elements<TableProperties>().First();
			TableGrid TableGrid = Table.Elements<TableGrid>().First();

			// TODO: Change exception types
			if (TableRows.Length != 1) {
				for (int j = 0; j < TableRows.Length; j++) {
					TableCell[] Cells = TableRows[j].Elements<TableCell>().ToArray();

					for (int i = 0; i < Cells.Length; i++)
						Process(Body, Cells[i]);
				}
			} else {
				TableRow TemplateRow = TableRows[0];
				TableCell[] Cells = TemplateRow.Elements<TableCell>().ToArray();

				WordTable WTable = WordTableContext.Begin();
				for (int i = 0; i < Cells.Length; i++) {
					WTable.BeginColumn();
					Process(Body, Cells[i]);
					WTable.EndColumn();
				}

				WordTable.Column[] Columns = WTable.GetColumns();
				WordTableContext.End();

				int RowCount = 0;

				if (Columns.Length > 0)
					RowCount = Columns.Select(C => (C.RefArray?.Count - 1) ?? 0).Max();

				List<TableCell> TableCells = new List<TableCell>();

				for (int i = 0; i < RowCount; i++) {
					TableCells.Clear();

					for (int j = 0; j < Columns.Length; j++) {
						JArray RefArray = Columns[j].RefArray;
						if (RefArray == null)
							continue;

						if (i < RefArray.Count - 1) {
							object Val = RefArray[i + 1][Columns[j].Identifier].Value<JValue>().Value;

							// TODO: Handle stuff other than strings?
							TableCells.Add(GenerateCell(Val.ToString()));
						} else
							TableCells.Add(GenerateCell(""));
					}

					Table.AppendChild(GenerateRow(TableCells.ToArray()));
				}
			}
		}

		void GetPageDimensions(Body Body, out PageDimensions Dim) {
			SectionProperties SectPr = Body.GetFirstChild<SectionProperties>();
			PageSize PgSz = SectPr.GetFirstChild<PageSize>();
			PageMargin Margin = SectPr.GetFirstChild<PageMargin>();
			Dim = new PageDimensions(PgSz.Width, PgSz.Height, Margin.Left, Margin.Right, Margin.Top, Margin.Bottom);
		}

		OpenXmlElement GenerateContent(Body Body, DataType DataType, object Obj, TokenParameters TokenParams) {
			switch (DataType) {
				case DataType.None:
					return null;

				case DataType.EmbeddedData: {
						EmbeddedData ObjData = (EmbeddedData)Obj;

						switch (ObjData.Type.MediaType) {
							case "image/jpeg":
							case "image/jpg":
							case "image/png": {
									Image Img = ObjData.ParseImageData();
									Utils.GetSizeInEMU(Img, out long W, out long H);

									const string WidthName = "width";
									const string HeightName = "height";

									if (TokenParams.Defined(WidthName) && TokenParams.Defined(HeightName)) {
										W = Utils.MilimeterToEmu(TokenParams.Get<int>(WidthName));
										H = Utils.MilimeterToEmu(TokenParams.Get<int>(HeightName));
									} else if (TokenParams.Defined(WidthName)) {
										long NewW = Utils.MilimeterToEmu(TokenParams.Get<int>(WidthName));
										Utils.Scale((float)NewW / W, ref W, ref H);
									} else if (TokenParams.Defined(HeightName)) {
										long NewH = Utils.MilimeterToEmu(TokenParams.Get<int>(HeightName));
										Utils.Scale((float)NewH / H, ref W, ref H);
									}

									GenerateImagePart(Img, out string ImageID);
									return new Run(new RunProperties(), GenerateDrawing(ImageID, W, H));
								}

							case "text/csv": {
									string CSVData = ObjData.ParseStringData();
									string[] CSVLines = CSVData.Replace("\r", "").Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

									// https://www.codeproject.com/Articles/1043875/Create-Word-table-using-OpenXML-and-Csharp-Without


									Table Tab = new Table();
									//TableProperties TblPr = new TableProperties();
									//Tab.AppendChild(TblPr);

									uint TableBorder = 1;
									BorderValues BorderType = BorderValues.BasicThinLines;

									TableProperties Props = new TableProperties();
									Tab.AppendChild(Props);

									PageDimensions Dim;
									GetPageDimensions(Body, out Dim);

									Props.AppendChild(new TableStyle() { Val = "0" });
									Props.AppendChild(new TableWidth() { Width = Dim.FillWidth.ToString(), Type = TableWidthUnitValues.Dxa });
									Props.AppendChild(new TableIndentation() { Width = 0, Type = TableWidthUnitValues.Dxa });

									TableBorders Borders = new TableBorders();
									Borders.AppendChild(new TopBorder() { Val = new EnumValue<BorderValues>(BorderType), Size = TableBorder });
									Borders.AppendChild(new BottomBorder() { Val = new EnumValue<BorderValues>(BorderType), Size = TableBorder });
									Borders.AppendChild(new LeftBorder() { Val = new EnumValue<BorderValues>(BorderType), Size = TableBorder });
									Borders.AppendChild(new RightBorder() { Val = new EnumValue<BorderValues>(BorderType), Size = TableBorder });
									Borders.AppendChild(new InsideHorizontalBorder() { Val = new EnumValue<BorderValues>(BorderType), Size = TableBorder });
									Borders.AppendChild(new InsideVerticalBorder() { Val = new EnumValue<BorderValues>(BorderType), Size = TableBorder });
									Props.AppendChild(Borders);

									Props.AppendChild(new TableLayout() { Type = TableLayoutValues.Fixed });

									foreach (var Line in CSVLines) {
										string[] Columns = Line.Split(new[] { ';' });
										TableCell[] Cells = new TableCell[Columns.Length - 1];

										for (int i = 0; i < Columns.Length - 1; i++) {
											string Column = Columns[i];

											if (Column.StartsWith("\"") && Column.EndsWith("\""))
												Column = Column.Substring(1, Column.Length - 2);

											Cells[i] = GenerateCell(Column);
										}

										Tab.AppendChild(GenerateRow(Cells));
									}

									//Body.ReplaceChild(Tab, Root);
									return Tab;
								}

							case "text/plain":
								return GenerateTextRun(ObjData.ParseStringData());

							default:
								throw new NotImplementedException();
						}
					}

				case DataType.String: {
						WordTable.ColumnStyle Style = WordTableContext.GetCurrent()?.GetCurrentColumn()?.ParseStyle() ?? WordTable.ColumnStyle.NONE;
						return GenerateTextRun(Obj.ToString(), Style);
					}

				case DataType.PageBreak:
					return new Run(new RunProperties(), new Break() { Type = BreakValues.Page });

				default:
					throw new NotImplementedException();
			}
		}

		ImagePart GenerateImagePart(Image Img, out string ID) {
			ImagePart ImgPart = MainPart.AddImagePart(ImagePartType.Png);

			using (MemoryStream Stream = new MemoryStream()) {
				Img.Save(Stream, ImageFormat.Png);
				Stream.Seek(0, SeekOrigin.Begin);

				ImgPart.FeedData(Stream);
			}


			ID = MainPart.GetIdOfPart(ImgPart);
			return ImgPart;
		}

		Drawing GenerateDrawing(string RelID, long W, long H, string Name = "", string Desc = "") {
			Dw.GraphicData GData = new Dw.GraphicData(new Dw.Pictures.Picture(
					new Dw.Pictures.NonVisualPictureProperties(
						new Dw.Pictures.NonVisualDrawingProperties() { Id = FreeIDSlot, Name = Name, Description = Desc },
						new Dw.Pictures.NonVisualPictureDrawingProperties(new Dw.PictureLocks() { NoChangeAspect = true, NoChangeArrowheads = true })
					),
					new Dw.Pictures.BlipFill(
						new Dw.Blip(new Dw.ExtensionList(
								new Dw.BlipExtension(new Dw10.UseLocalDpi() { Val = false }) { Uri = "{28A0092B-C50C-407E-A947-70E740481C1C}" }
							)) {
							Embed = RelID
						},
						new Dw.SourceRectangle(),
						new Dw.Stretch(new Dw.FillRectangle())
					),
					new Dw.Pictures.ShapeProperties(
							new Dw.Transform2D(new Dw.Offset() { X = 0L, Y = 0L },
							new Dw.Extents() { Cx = W, Cy = H }),
							new Dw.PresetGeometry(new Dw.AdjustValueList()) { Preset = Dw.ShapeTypeValues.Rectangle },
							new Dw.NoFill(),
							new Dw.Outline(new Dw.NoFill())
						) {
						BlackWhiteMode = Dw.BlackWhiteModeValues.Auto
					}
				)) {
				Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture"
			};

			Drawing DrawingElement = new Drawing(
				new Dw.Wordprocessing.Inline(
					new Dw.Wordprocessing.Extent() { Cx = W, Cy = H },
					new Dw.Wordprocessing.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
					new Dw.Wordprocessing.DocProperties() { Id = FreeIDSlot, Name = Name, Description = Desc },
					new Dw.Wordprocessing.NonVisualGraphicFrameDrawingProperties(new Dw.GraphicFrameLocks() { NoChangeAspect = true }),
					new Dw.Graphic(GData)
					) {
					DistanceFromTop = 0U,
					DistanceFromBottom = 0U,
					DistanceFromLeft = 0U,
					DistanceFromRight = 0U
				});

			FreeIDSlot++;
			return DrawingElement;
		}

		TableRow GenerateRow(TableCell[] Cells) {
			TableRow Row = new TableRow();

			for (int i = 0; i < Cells.Length; i++) {
				Row.AppendChild(Cells[i]);
			}

			return Row;
		}

		TableCell GenerateCell(OpenXmlElement Content) {
			TableCell Cell = new TableCell();
			Cell.AppendChild(new TableCellProperties());
			Cell.Append(Content);
			return Cell;
		}

		TableCell GenerateCell(string Text) {
			return GenerateCell(new Paragraph(GenerateTextRun(Text)));
		}

		Run GenerateTextRun(string Content, WordTable.ColumnStyle Style = WordTable.ColumnStyle.NONE) {
			RunProperties Props = CurrentRun.Copy() ?? new RunProperties();

			if (Style.HasFlag(WordTable.ColumnStyle.BOLD) && !Props.HasChild<Bold>())
				Props.AppendChild(new Bold());

			if (Style.HasFlag(WordTable.ColumnStyle.ITALIC) && !Props.HasChild<Italic>())
				Props.AppendChild(new Italic());

			if (Style.HasFlag(WordTable.ColumnStyle.UNDERLINE) && !Props.HasChild<Underline>())
				Props.AppendChild(new Underline());

			Run NewRun = new Run();
			NewRun.AppendChild(Props);

			string[] Lines = Content.Replace("\r", "").Split(new[] { '\n' });
			for (int i = 0; i < Lines.Length; i++) {
				NewRun.AppendChild(new Text(Lines[i]) { Space = SpaceProcessingModeValues.Preserve });

				if (i < Lines.Length - 1)
					NewRun.AppendChild(new Break());
			}

			return NewRun;
		}

		static bool ParentsAreElement<T>(OpenXmlElement E, out OpenXmlElement FoundParent) where T : OpenXmlElement {
			OpenXmlElement Parent = E.Parent;
			FoundParent = null;

			while (Parent != null) {
				if (Parent is T) {
					FoundParent = Parent;
					return true;
				}

				if (Parent is Body)
					return false;

				Parent = Parent.Parent;
			}

			return false;
		}

		/// <param name="Token">Reference token to check, strips leading and tailing braces</param>
		/// <param name="TokenParams">Class containing parsed token parameters, may be empty</param>
		/// <returns></returns>
		static bool VerifyToken(ref string Token, out TokenParameters TokenParams) {
			// Token should start with { and end with }, should not contain any other braces in the middle
			// TODO: Check for valid characters only A-Z a-z . , $ ( ) = 0-9

			const string LEAD_BRACE = "{";
			const string TAIL_BRACE = "}";
			const string LEAD_PAREN = "(";
			const string TAIL_PAREN = ")";

			TokenParams = new TokenParameters();

			if (string.IsNullOrWhiteSpace(Token))
				return false;

			if (Token.StartsWith("{{") && Token.EndsWith("}}"))
				return false;

			if (Token.StartsWith(LEAD_BRACE) && Token.EndsWith(TAIL_BRACE)) {
				string TempToken = Token.Substring(1, Token.Length - 2);

				if (TempToken.Contains(LEAD_BRACE) || TempToken.Contains(TAIL_BRACE))
					return false;

				// Token parameters
				if (TempToken.Contains(LEAD_PAREN) && TempToken.Contains(TAIL_PAREN)) {
					string TokenParamsString = TempToken.Substring(TempToken.IndexOf(LEAD_PAREN) + 1);
					TokenParamsString = TokenParamsString.Substring(0, TokenParamsString.LastIndexOf(TAIL_PAREN));
					TokenParams = new TokenParameters(TokenParamsString);

					TempToken = TempToken.Substring(0, TempToken.IndexOf(LEAD_PAREN));
				}

				Token = TempToken;
				return true;
			}

			return false;
		}

		static bool VerifyCodeToken(ref string Token) {
			const string LEAD = "{{";
			const string TAIL = "}}";

			if (Token.StartsWith(LEAD) && Token.EndsWith(TAIL)) {
				string TempToken = Token.Substring(LEAD.Length, Token.Length - LEAD.Length - TAIL.Length);

				if (TempToken.Contains(LEAD) || TempToken.Contains(TAIL))
					return false;

				Token = TempToken.Trim();
				return true;
			}

			return false;
		}
	}
}