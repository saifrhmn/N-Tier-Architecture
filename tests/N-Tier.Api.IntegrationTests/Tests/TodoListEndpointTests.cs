﻿using FizzWare.NBuilder;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using N_Tier.Application.Models;
using N_Tier.Application.Models.TodoList;
using N_Tier.Core.Entities;
using N_Tier.DataAccess.Persistence;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using N_Tier.Api.IntegrationTests.Config;
using N_Tier.Api.IntegrationTests.Helpers;

namespace N_Tier.Api.IntegrationTests.Tests
{
    [TestFixture]
    public class TodoListEndpointTests : BaseOneTimeSetup
    {
        [Test]
        public async Task Create_Should_Add_TodoList_In_Database()
        {
            // Arrange
            var host = _host;

            var context = _host.Services.GetRequiredService<DatabaseContext>();

            var createTodoListModel = Builder<CreateTodoListModel>.CreateNew().Build();

            // Act
            var apiResponse = await _client.PostAsync("/api/todoLists", new JsonContent(createTodoListModel));

            // Assert
            var response = JsonConvert.DeserializeObject<ApiResult<CreateTodoListResponseModel>>(await apiResponse.Content.ReadAsStringAsync());
            var todoListFromDatabase = await context.TodoLists.Where(u => u.Id == response.Result.Id).FirstOrDefaultAsync();
            CheckResponse.Succeded(response, 201);
            todoListFromDatabase.Should().NotBeNull();
            todoListFromDatabase.Title.Should().Be(createTodoListModel.Title);
        }

        [Test]
        public async Task Create_Should_Return_BadRequest_If_Title_Is_Incorrect()
        {
            // Arrange
            var context = _host.Services.GetRequiredService<DatabaseContext>();

            var createTodoListModel = Builder<CreateTodoListModel>.CreateNew().With(ctl => ctl.Title = "1").Build();

            // Act
            var apiResponse = await _client.PostAsync("/api/todoLists", new JsonContent(createTodoListModel));

            // Assert
            var response = JsonConvert.DeserializeObject<ApiResult<string>>(await apiResponse.Content.ReadAsStringAsync());
            var todoListFromDatabase = await context.TodoLists.Where(tl => tl.Title == createTodoListModel.Title).FirstOrDefaultAsync();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            CheckResponse.Failure(response, 400);
            todoListFromDatabase.Should().BeNull();
        }

        [Test]
        public async Task Update_Should_Update_Todo_List_From_Database()
        {
            // Arrange
            var context = _host.Services.GetRequiredService<DatabaseContext>();

            var user = await context.Users.Where(u => u.Email == "nuyonu@gmail.com").FirstOrDefaultAsync();

            var todoListFromDatabase = context.TodoLists.Add(Builder<TodoList>.CreateNew().With(tl => tl.Id = Guid.NewGuid()).With(tl => tl.CreatedBy = user.Id).Build()).Entity;

            context.SaveChanges();

            var updateTodoListModel = Builder<UpdateTodoListModel>.CreateNew().With(utl => utl.Title = "UpdateTodoListTitleIntegration").Build();

            // Act
            var apiResponse = await _client.PutAsync($"/api/todoLists/{todoListFromDatabase.Id}", new JsonContent(updateTodoListModel));

            // Assert
            context = (await GetNewHostAsync()).Services.GetRequiredService<DatabaseContext>();
            var response = JsonConvert.DeserializeObject<ApiResult<UpdateTodoListResponseModel>>(await apiResponse.Content.ReadAsStringAsync());
            var updatedTodoListFromDatabase = await context.TodoLists.Where(tl => tl.Id == response.Result.Id).FirstOrDefaultAsync();
            CheckResponse.Succeded(response);
            updatedTodoListFromDatabase.Should().NotBeNull();
            updatedTodoListFromDatabase.Title.Should().Be(updateTodoListModel.Title);
        }

        [Test]
        public async Task Update_Should_Return_NotFound_If_Todo_List_Does_Not_Exist_Anymore()
        {
            // Arrange
            var context = _host.Services.GetRequiredService<DatabaseContext>();

            var updateTodoListModel = Builder<UpdateTodoListModel>.CreateNew().With(utl => utl.Title = "UpdateTodoListIntegration").Build();

            // Act
            var apiResponse = await _client.PutAsync($"/api/todoLists/{Guid.NewGuid()}", new JsonContent(updateTodoListModel));

            // Assert
            var response = JsonConvert.DeserializeObject<ApiResult<string>>(await apiResponse.Content.ReadAsStringAsync());
            var updatedTodoListFromDatabase = await context.TodoLists.Where(tl => tl.Title == updateTodoListModel.Title).FirstOrDefaultAsync();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            CheckResponse.Failure(response, 404);
            updatedTodoListFromDatabase.Should().BeNull();
        }

        [Test]
        public async Task Update_Should_Return_BadRequest_If_Todo_List_Does_Not_Belong_To_User()
        {
            // Arrange  
            var context = _host.Services.GetRequiredService<DatabaseContext>();

            var todoListFromDatabase = context.TodoLists.Add(Builder<TodoList>.CreateNew().With(tl => tl.Id = Guid.NewGuid()).Build()).Entity;

            context.SaveChanges();

            var updateTodoListModel = Builder<UpdateTodoListModel>.CreateNew().Build();

            // Act
            var apiResponse = await _client.PutAsync($"/api/todoLists/{todoListFromDatabase.Id}", new JsonContent(updateTodoListModel));

            // Assert
            var response = JsonConvert.DeserializeObject<ApiResult<string>>(await apiResponse.Content.ReadAsStringAsync());
            var updatedTodoListFromDatabase = await context.TodoLists.Where(tl => tl.Title == updateTodoListModel.Title).FirstOrDefaultAsync();
            apiResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            CheckResponse.Failure(response, 400);
            updatedTodoListFromDatabase.Should().NotBeNull();
            updatedTodoListFromDatabase.Title.Should().Be(todoListFromDatabase.Title);
        }

        [Test]
        public async Task Delete_Should_Delete_Todo_List_From_Database()
        {
            // Arrange
            var context = _host.Services.GetRequiredService<DatabaseContext>();

            var user = await context.Users.Where(u => u.Email == "nuyonu@gmail.com").FirstOrDefaultAsync();

            var todoListFromDatabase = context.TodoLists.Add(Builder<TodoList>.CreateNew().With(tl => tl.Id = Guid.NewGuid()).With(tl => tl.CreatedBy = user.Id).Build()).Entity;

            context.SaveChanges();

            var updateTodoListModel = Builder<UpdateTodoListModel>.CreateNew().Build();

            // Act
            var apiResponse = await _client.DeleteAsync($"/api/todoLists/{todoListFromDatabase.Id}");

            // Assert
            var response = JsonConvert.DeserializeObject<ApiResult<BaseResponseModel>>(await apiResponse.Content.ReadAsStringAsync());
            var updatedTodoListFromDatabase = await context.TodoLists.Where(tl => tl.Id == response.Result.Id).FirstOrDefaultAsync();
            CheckResponse.Succeded(response);
            updatedTodoListFromDatabase.Should().BeNull();
        }

        [Test]
        public async Task Delete_Should_Return_NotFound_If_Todo_List_Does_Not_Exist_Anymore()
        {
            // Arrange

            // Act
            var apiResponse = await _client.DeleteAsync($"/api/todoLists/{Guid.NewGuid()}");

            // Assert
            var response = JsonConvert.DeserializeObject<ApiResult<string>>(await apiResponse.Content.ReadAsStringAsync());
            apiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
            CheckResponse.Failure(response, 404);
        }

        [Test]
        public async Task Get_Todo_Lists_Should_Return_All_Todo_Lists_For_Specified_User_From_Database()
        {
            // Arrange
            var context = _host.Services.GetRequiredService<DatabaseContext>();

            var user = await context.Users.Where(u => u.Email == "nuyonu@gmail.com").FirstOrDefaultAsync();

            context.TodoLists.RemoveRange(context.TodoLists.ToList());

            var todoLists = Builder<TodoList>.CreateListOfSize(10).All().With(tl => tl.Id = Guid.NewGuid()).With(tl => tl.CreatedBy = user.Id).Build();
            var todoListsNotBelongToTheUser = Builder<TodoList>.CreateListOfSize(10).All()
                                                .With(tl => tl.Id = Guid.NewGuid()).With(tl => tl.CreatedBy = Guid.NewGuid().ToString()).Build();

            context.TodoLists.AddRange(todoLists);
            context.TodoLists.AddRange(todoListsNotBelongToTheUser);

            context.SaveChanges();

            // Act
            var apiResponse = await _client.GetAsync($"/api/todoLists");

            // Assert
            var response = JsonConvert.DeserializeObject<ApiResult<IEnumerable<TodoListResponseModel>>>(await apiResponse.Content.ReadAsStringAsync());
            CheckResponse.Succeded(response);
            response.Result.Should().HaveCount(10);
        }
    }
}
