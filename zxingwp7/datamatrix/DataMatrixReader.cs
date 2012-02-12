/*
* Copyright 2007 ZXing authors
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using zxingwp7.common;
using zxingwp7.datamatrix.decoder;
using zxingwp7.datamatrix.detector;

namespace zxingwp7.datamatrix
{
    /// <summary> This implementation can detect and decode Data Matrix codes in an image.
    /// 
    /// </summary>
    /// <author>  bbrown@google.com (Brian Brown)
    /// </author>
    /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source 
    /// </author>
    public sealed class DataMatrixReader : Reader
    {

        private static readonly ResultPoint[] NO_POINTS = new ResultPoint[0];


        private readonly Decoder decoder = new Decoder();

        #region Reader Members

        /// <summary> Locates and decodes a Data Matrix code in an image.
        /// 
        /// </summary>
        /// <returns> a String representing the content encoded by the Data Matrix code
        /// </returns>
        /// <throws>  ReaderException if a Data Matrix code cannot be found, or cannot be decoded </throws>
        public Result decode(BinaryBitmap image)
        {
            return decode(image, null);
        }

        #endregion

        public Result decode(BinaryBitmap image, Dictionary<DecodeHintType, Object> hints)
        {
            DecoderResult decoderResult;
            ResultPoint[] points;
            if (hints != null && hints.ContainsKey(DecodeHintType.PURE_BARCODE))
            {
                BitMatrix bits = extractPureBits(image.BlackMatrix);
                decoderResult = decoder.decode(bits);
                points = NO_POINTS;
            }
            else
            {
                DetectorResult detectorResult = new Detector(image.BlackMatrix).detect();
                decoderResult = decoder.decode(detectorResult.Bits);
                points = detectorResult.Points;
            }
            var result = new Result(decoderResult.Text, decoderResult.RawBytes, points, BarcodeFormat.DATAMATRIX);
            if (decoderResult.ByteSegments != null)
            {
                result.putMetadata(ResultMetadataType.BYTE_SEGMENTS, decoderResult.ByteSegments);
            }
            if (decoderResult.ECLevel != null)
            {
                result.putMetadata(ResultMetadataType.ERROR_CORRECTION_LEVEL, decoderResult.ECLevel.ToString());
            }
            return result;
        }

        /// <summary> This method detects a Data Matrix code in a "pure" image -- that is, pure monochrome image
        /// which contains only an unrotated, unskewed, image of a Data Matrix code, with some white border
        /// around it. This is a specialized method that works exceptionally fast in this special
        /// case.
        /// </summary>
        private static BitMatrix extractPureBits(BitMatrix image)
        {
            // Now need to determine module size in pixels

            int height = image.Height;
            int width = image.Width;
            int minDimension = Math.Min(height, width);

            // First, skip white border by tracking diagonally from the top left down and to the right:
            int borderWidth = 0;
            while (borderWidth < minDimension && !image.get_Renamed(borderWidth, borderWidth))
            {
                borderWidth++;
            }
            if (borderWidth == minDimension)
            {
                throw ReaderException.Instance;
            }

            // And then keep tracking across the top-left black module to determine module size
            int moduleEnd = borderWidth + 1;
            while (moduleEnd < width && image.get_Renamed(moduleEnd, borderWidth))
            {
                moduleEnd++;
            }
            if (moduleEnd == width)
            {
                throw ReaderException.Instance;
            }

            int moduleSize = moduleEnd - borderWidth;

            // And now find where the bottommost black module on the first column ends
            int columnEndOfSymbol = height - 1;
            while (columnEndOfSymbol >= 0 && !image.get_Renamed(borderWidth, columnEndOfSymbol))
            {
                columnEndOfSymbol--;
            }
            if (columnEndOfSymbol < 0)
            {
                throw ReaderException.Instance;
            }
            columnEndOfSymbol++;

            // Make sure width of barcode is a multiple of module size
            if ((columnEndOfSymbol - borderWidth)%moduleSize != 0)
            {
                throw ReaderException.Instance;
            }
            int dimension = (columnEndOfSymbol - borderWidth)/moduleSize;

            // Push in the "border" by half the module width so that we start
            // sampling in the middle of the module. Just in case the image is a
            // little off, this will help recover.
            borderWidth += (moduleSize >> 1);

            int sampleDimension = borderWidth + (dimension - 1)*moduleSize;
            if (sampleDimension >= width || sampleDimension >= height)
            {
                throw ReaderException.Instance;
            }

            // Now just read off the bits
            var bits = new BitMatrix(dimension);
            for (int i = 0; i < dimension; i++)
            {
                int iOffset = borderWidth + i*moduleSize;
                for (int j = 0; j < dimension; j++)
                {
                    if (image.get_Renamed(borderWidth + j*moduleSize, iOffset))
                    {
                        bits.set_Renamed(j, i);
                    }
                }
            }
            return bits;
        }
    }
}