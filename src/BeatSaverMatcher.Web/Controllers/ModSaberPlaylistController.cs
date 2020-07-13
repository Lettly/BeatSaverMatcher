﻿using System;
using System.Linq;
using System.Threading.Tasks;
using BeatSaverMatcher.Web.Models;
using BeatSaverMatcher.Web.Result;
using Microsoft.AspNetCore.Mvc;

namespace BeatSaverMatcher.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ModSaberPlaylistController : ControllerBase
    {
        private readonly WorkItemStore _itemStore;
        private readonly SpotifyRepository _spotifyRepository;

        public ModSaberPlaylistController(WorkItemStore itemStore, SpotifyRepository spotifyRepository)
        {
            _itemStore = itemStore;
            _spotifyRepository = spotifyRepository;
        }

        [HttpGet("{playlistId}")]
        [HttpGet("{playlistId}/{keys}")]
        public async Task<ActionResult<ModSaberPlaylist>> GetMatchesAsPlaylistDownload([FromRoute] string playlistId, [FromRoute] string keys)
        {
            var workItem = _itemStore.Get(playlistId);
            if (workItem == null)
                NotFound();

            if (workItem.State != SongMatchState.Finished)
                BadRequest();

            var playlist = await _spotifyRepository.GetPlaylist(playlistId);

            var beatmaps = workItem.Result.Matches.SelectMany(x => x.BeatMaps).ToList();
            if (keys != null)
            {
                var keysList = keys.Split(',');
                beatmaps = beatmaps.Where(x => keysList.Contains(x.BeatSaverKey.ToString("x"))).ToList();
            }

            return new ModSaberPlaylist
            {
                PlaylistTitle = playlist.Name,
                PlaylistAuthor = (playlist.Owner?.DisplayName ?? "") + " using https://github.com/patagonaa/BeatSaverMatcher",
                Image = null, //TODO
                Songs = beatmaps.Select(x => new ModSaberSong
                {
                    Hash = string.Join("", x.Hash.Select(x => x.ToString("x2"))),
                    Key = x.BeatSaverKey.ToString("x"),
                    SongName = x.Name,
                    Uploader = x.Uploader
                }).ToList()
            };
        }
    }
}
