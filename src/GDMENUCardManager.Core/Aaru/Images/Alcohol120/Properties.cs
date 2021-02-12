// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Properties.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains properties Alcohol 120% disc images.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2021 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
//using Schemas;

namespace Aaru.DiscImages
{
    public sealed partial class Alcohol120
    {
        public OpticalImageCapabilities OpticalCapabilities => OpticalImageCapabilities.CanStoreAudioTracks  |
                                                               OpticalImageCapabilities.CanStoreDataTracks   |
                                                               OpticalImageCapabilities.CanStoreSubchannelRw |
                                                               OpticalImageCapabilities.CanStoreSessions     |
                                                               OpticalImageCapabilities.CanStoreIsrc         |
                                                               OpticalImageCapabilities.CanStoreCdText       |
                                                               OpticalImageCapabilities.CanStoreMcn          |
                                                               OpticalImageCapabilities.CanStoreRawData      |
                                                               OpticalImageCapabilities.CanStoreCookedData   |
                                                               OpticalImageCapabilities.CanStoreMultipleTracks;
        public ImageInfo Info   => _imageInfo;
        public string    Name   => "Alcohol 120% Media Descriptor Structure";
        public Guid      Id     => new Guid("A78FBEBA-0307-4915-BDE3-B8A3B57F843F");
        public string    Author => "Natalia Portillo";

        public string Format => "Alcohol 120% Media Descriptor Structure";

        public List<Partition> Partitions { get; private set; }

        public List<CommonTypes.Structs.Track> Tracks
        {
            get
            {
                List<CommonTypes.Structs.Track> tracks = new List<CommonTypes.Structs.Track>();

                if (_alcTracks == null)
                    return tracks;

                foreach(Track alcTrack in _alcTracks.Values)
                {
                    ushort sessionNo =
                        (from session in Sessions
                         where alcTrack.point >= session.StartTrack || alcTrack.point <= session.EndTrack
                         select session.SessionSequence).FirstOrDefault();

                    if(!_alcTrackExtras.TryGetValue(alcTrack.point, out TrackExtra alcExtra))
                        continue;

                    var aaruTrack = new CommonTypes.Structs.Track
                    {
                        TrackStartSector       = alcTrack.startLba,
                        TrackEndSector         = alcTrack.startLba + alcExtra.sectors - 1,
                        TrackPregap            = alcExtra.pregap,
                        TrackSession           = sessionNo,
                        TrackSequence          = alcTrack.point,
                        TrackType              = TrackModeToTrackType(alcTrack.mode),
                        TrackFilter            = _alcImage,
                        TrackFile              = _alcImage.GetFilename(),
                        TrackFileOffset        = alcTrack.startOffset,
                        TrackFileType          = "BINARY",
                        TrackRawBytesPerSector = alcTrack.sectorSize,
                        TrackBytesPerSector    = TrackModeToCookedBytesPerSector(alcTrack.mode)
                    };

                    if(alcExtra.pregap > 0)
                        aaruTrack.Indexes.Add(0, (int)(alcTrack.startLba - alcExtra.pregap));

                    aaruTrack.Indexes.Add(1, (int)alcTrack.startLba);

                    switch(alcTrack.subMode)
                    {
                        case SubchannelMode.Interleaved:
                            aaruTrack.TrackSubchannelFilter = _alcImage;
                            aaruTrack.TrackSubchannelFile   = _alcImage.GetFilename();
                            aaruTrack.TrackSubchannelOffset = alcTrack.startOffset;
                            aaruTrack.TrackSubchannelType   = TrackSubchannelType.RawInterleaved;

                            break;
                        case SubchannelMode.None:
                            aaruTrack.TrackSubchannelType = TrackSubchannelType.None;

                            break;
                    }

                    if(_header.type != MediumType.CD  &&
                       _header.type != MediumType.CDR &&
                       _header.type != MediumType.CDRW)
                    {
                        aaruTrack.TrackPregap = 0;
                        aaruTrack.Indexes?.Clear();
                    }

                    tracks.Add(aaruTrack);
                }

                return tracks;
            }
        }

        public List<CommonTypes.Structs.Session> Sessions { get; private set; }

        //public List<DumpHardwareType> DumpHardware => null;
        //public CICMMetadataType       CicmMetadata => null;

        //public IEnumerable<MediaTagType> SupportedMediaTags => new[]
        //{
        //    MediaTagType.CD_FullTOC, MediaTagType.DVD_BCA, MediaTagType.DVD_DMI, MediaTagType.DVD_PFI
        //};
        //public IEnumerable<SectorTagType> SupportedSectorTags => new[]
        //{
        //    SectorTagType.CdSectorEcc, SectorTagType.CdSectorEccP, SectorTagType.CdSectorEccQ,
        //    SectorTagType.CdSectorEdc, SectorTagType.CdSectorHeader, SectorTagType.CdSectorSubHeader,
        //    SectorTagType.CdSectorSync, SectorTagType.CdTrackFlags, SectorTagType.CdSectorSubchannel
        //};
        //public IEnumerable<MediaType> SupportedMediaTypes => new[]
        //{
        //    MediaType.BDR, MediaType.BDRE, MediaType.BDREXL, MediaType.BDROM, MediaType.BDRXL, MediaType.CBHD,
        //    MediaType.CD, MediaType.CDDA, MediaType.CDEG, MediaType.CDG, MediaType.CDI, MediaType.CDMIDI,
        //    MediaType.CDMRW, MediaType.CDPLUS, MediaType.CDR, MediaType.CDROM, MediaType.CDROMXA, MediaType.CDRW,
        //    MediaType.CDV, MediaType.DVDDownload, MediaType.DVDPR, MediaType.DVDPRDL, MediaType.DVDPRW,
        //    MediaType.DVDPRWDL, MediaType.DVDR, MediaType.DVDRAM, MediaType.DVDRDL, MediaType.DVDROM, MediaType.DVDRW,
        //    MediaType.DVDRWDL, MediaType.EVD, MediaType.FDDVD, MediaType.DTSCD, MediaType.FVD, MediaType.HDDVDR,
        //    MediaType.HDDVDRAM, MediaType.HDDVDRDL, MediaType.HDDVDROM, MediaType.HDDVDRW, MediaType.HDDVDRWDL,
        //    MediaType.HDVMD, MediaType.HVD, MediaType.JaguarCD, MediaType.MEGACD, MediaType.PS1CD, MediaType.PS2CD,
        //    MediaType.PS2DVD, MediaType.PS3BD, MediaType.PS3DVD, MediaType.PS4BD, MediaType.SuperCDROM2, MediaType.SVCD,
        //    MediaType.SVOD, MediaType.SATURNCD, MediaType.ThreeDO, MediaType.UDO, MediaType.UDO2, MediaType.UDO2_WORM,
        //    MediaType.UMD, MediaType.VCD, MediaType.VCDHD, MediaType.NeoGeoCD, MediaType.PCFX, MediaType.CDTV,
        //    MediaType.CD32, MediaType.Nuon, MediaType.Playdia, MediaType.Pippin, MediaType.FMTOWNS, MediaType.MilCD,
        //    MediaType.VideoNow, MediaType.VideoNowColor, MediaType.VideoNowXp, MediaType.CVD, MediaType.PCD
        //};
        //public IEnumerable<(string name, Type type, string description, object @default)> SupportedOptions =>
        //    new (string name, Type type, string description, object @default)[]
        //        {};
        //public IEnumerable<string> KnownExtensions => new[]
        //{
        //    ".mds"
        //};
        //public bool   IsWriting    { get; private set; }
        //public string ErrorMessage { get; private set; }
    }
}