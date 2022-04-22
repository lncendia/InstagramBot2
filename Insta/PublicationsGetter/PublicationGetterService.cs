#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Classes.ResponseWrappers;
using InstagramApiSharp.Helpers;
using Newtonsoft.Json;

namespace Insta.PublicationsGetter
{
    public static class WebApi
    {
        public static async Task<IResult<InstaSectionMedia>> GetReelsHashtagMediaListAsync(this IInstaApi api,
            string tag, PaginationParameters? parameters, IRequestDelay delay, CancellationToken token)
        {
            UserAuthValidator.Validate(api.GetLoggedUser(), api.IsUserAuthenticated);
            try
            {
                parameters ??= PaginationParameters.MaxPagesToLoad(1);
                var list = new List<InstaSectionMediaResponse>();
                InstaSectionMediaListResponse? data;
                do
                {
                    await Task.Delay(delay.Value, token);
                    var uri = new Uri($"https://i.instagram.com/api/v1/tags/{tag}/sections/");
                    var dictionary = new Dictionary<string, string>
                    {
                        {"include_persistent", "false"},
                        {"tab", "clips"}
                    };
                    if (!string.IsNullOrEmpty(parameters.NextMaxId)) dictionary.Add("max_id", parameters.NextMaxId);
                    if (parameters.NextMediaIds != null)
                        dictionary.Add("next_media_ids", JsonConvert.SerializeObject(parameters.NextMediaIds));
                    var result = await GetPosts(uri, dictionary, api);
                    if (!result.Succeeded)
                    {
                        return result.Info.ResponseType == ResponseType.InternalException
                            ? Result.Fail<InstaSectionMedia>(result.Info.Exception)
                            : Result.Fail<InstaSectionMedia>(result.Info.Message);
                    }

                    data = JsonConvert.DeserializeObject<InstaSectionMediaListResponse>(result.Value);
                    if (data is not {Status: "ok"})
                    {
                        return Result.UnExpectedResponse<InstaSectionMedia>(new HttpResponseMessage(),
                            result.Value);
                    }

                    list.AddRange(data.Sections);
                    parameters.NextMediaIds = data.NextMediaIds;
                    parameters.PagesLoaded++;
                    parameters.NextMaxId = data.NextMaxId;
                    parameters.NextPage = data.NextPage;
                } while (!string.IsNullOrEmpty(parameters.NextMaxId)
                         && parameters.PagesLoaded <= parameters.MaximumPagesToLoad);

                data.Sections = list;
                return Result.Success(GetMedia(data));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaSectionMedia), ResponseType.NetworkProblem)!;
            }
            catch (Exception exception)
            {
                return Result.Fail(exception, default(InstaSectionMedia))!;
            }
        }

        public static async Task<IResult<InstaSectionMedia>> GetRecentHashtagMediaListAsync(this IInstaApi api,
            string tag, PaginationParameters? parameters, IRequestDelay delay, CancellationToken token)
        {
            UserAuthValidator.Validate(api.GetLoggedUser(), api.IsUserAuthenticated);
            try
            {
                parameters ??= PaginationParameters.MaxPagesToLoad(1);
                var list = new List<InstaSectionMediaResponse>();
                InstaSectionMediaListResponse? data;
                do
                {
                    await Task.Delay(delay.Value, token);
                    var uri = new Uri($"https://i.instagram.com/api/v1/tags/{tag}/sections/");
                    var dictionary = new Dictionary<string, string>
                    {
                        {"include_persistent", "false"},
                        {"tab", "recent"}
                    };
                    if (!String.IsNullOrEmpty(parameters.NextMaxId)) dictionary.Add("max_id", parameters.NextMaxId);
                    if (parameters.NextMediaIds != null)
                        dictionary.Add("next_media_ids", JsonConvert.SerializeObject(parameters.NextMediaIds));
                    var result = await GetPosts(uri, dictionary, api);

                    if (!result.Succeeded)
                    {
                        return result.Info.ResponseType == ResponseType.InternalException
                            ? Result.Fail<InstaSectionMedia>(result.Info.Exception)
                            : Result.Fail<InstaSectionMedia>(result.Info.Message);
                    }

                    data = JsonConvert.DeserializeObject<InstaSectionMediaListResponse>(result.Value);
                    if (data is not {Status: "ok"})
                    {
                        return Result.UnExpectedResponse<InstaSectionMedia>(new HttpResponseMessage(),
                            result.Value);
                    }

                    list.AddRange(data.Sections);
                    parameters.NextMediaIds = data.NextMediaIds;
                    parameters.PagesLoaded++;
                    parameters.NextMaxId = data.NextMaxId;
                    parameters.NextPage = data.NextPage;
                } while (!string.IsNullOrEmpty(parameters.NextMaxId)
                         && parameters.PagesLoaded <= parameters.MaximumPagesToLoad);

                data.Sections = list;
                return Result.Success(GetMedia(data));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException httpException)
            {
                return Result.Fail(httpException, default(InstaSectionMedia), ResponseType.NetworkProblem)!;
            }
            catch (Exception exception)
            {
                return Result.Fail(exception, default(InstaSectionMedia))!;
            }
        }

        private static async Task<IResult<string>> GetPosts(Uri uri, Dictionary<string, string> dictionary,
            IInstaApi api)
        {
            int countFail = 0;
            IResult<string> result;
            do
            {
                result = await api.SendPostRequestAsync(uri, dictionary.ToDictionary(x => x.Key, x => x.Value));
                if (result.Info.Message == "checkpoint_required") countFail++;
                else break;
            } while (countFail < 6);

            return result;
        }

        private static InstaSectionMedia GetMedia(InstaSectionMediaListResponse data)
        {
            var fabric = Type.GetType("InstagramApiSharp.Converters.ConvertersFabric, InstagramApiSharp", true,
                    true)!
                .GetProperty("Instance")
                ?.GetGetMethod()
                ?.Invoke(null, null);
            if (fabric == null) throw new Exception("Failed to get a converters fabric.");
            var medias = fabric.GetType().GetMethod("GetHashtagMediaListConverter")
                ?.Invoke(fabric, new[] {(object) data});
            if (medias == null) throw new Exception("Failed to get a converter.");
            return (InstaSectionMedia) medias.GetType().GetMethod("Convert")?.Invoke(medias, null)!;
        }
    }
}