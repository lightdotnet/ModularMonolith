import { identityApi } from "@/lib/server/backend-api";

const { requestJson } = identityApi;
import { guardCall, guardResponseCall } from "@/lib/server/call-guard";
import type { ApiResponse, PagedResult, Result } from "@/types/api";
import type {
  CreateUserRequest,
  DomainUserDto,
  SearchUsersParams,
  UserDto,
} from "@/modules/identity/users/types/user";

export function getAllUsers() {
  return guardCall(() => requestJson<Result<UserDto[]>>("user"));
}

export function getDomainUser(userName: string) {
  return guardCall(() =>
    requestJson<Result<DomainUserDto>>(`user/get_domain_user/${userName}`),
  );
}

export function getUserById(id: string) {
  return guardCall(() => requestJson<Result<UserDto>>(`user/${id}`));
}

export function searchUsers(params: SearchUsersParams = {}) {
  return guardCall(() =>
    requestJson<PagedResult<UserDto>>("user/search", {
      method: "GET",
      query: {
        searchValue: params.searchValue,
        pageNumber: String(params.pageNumber ?? 1),
        pageSize: String(params.pageSize ?? 20),
      },
    }),
  );
}

export function createUser(request: CreateUserRequest) {
  return guardCall(() =>
    requestJson<Result<string>>("user", { method: "POST", body: request }),
  );
}

export function updateUser(user: UserDto) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`user/${user.id}`, { method: "PUT", body: user }),
  );
}

export function forcePassword(id: string, password: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`user/${id}/password/force`, {
      method: "PUT",
      body: password,
    }),
  );
}

export function deleteUser(id: string) {
  return guardResponseCall(() =>
    requestJson<ApiResponse>(`user/${id}`, { method: "DELETE" }),
  );
}
