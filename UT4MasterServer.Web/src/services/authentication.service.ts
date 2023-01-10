import { SessionStore as SessionStore } from '../stores/session-store';
import { ILoginRequest } from '../types/login-request';
import HttpService from './http.service';
import { ISession } from '../types/session';
import { GrantType } from '../enums/grant-type';
import { IRefreshSessionRequest } from '../types/refresh-session-request';
import { IAuthCodeResponse } from '../types/auth-code-response';

export default class AuthenticationService extends HttpService {
    private sessionUrl = `${__BACKEND_URL}/account/api/oauth/token`;
    private authCode = `${__BACKEND_URL}/account/api/oauth/auth`;

    async passwordLogin(request: ILoginRequest) {
        try {
            const session = await this.postForm<ISession, ILoginRequest>(this.sessionUrl, {
                body: request,
                headers: {
                    'Authorization': `${__WEB_BASIC_AUTH}`
                }
            });
            console.debug('Password login response: ', session);
            SessionStore.session = session;
        }
        catch (err: unknown) {
            SessionStore.session = null;
        }
    }

    async refreshSession(): Promise<boolean> {
        const tokenValid = SessionStore.session && SessionStore.session.refresh_token && new Date() < new Date(SessionStore.session.refresh_expires_at);
        if (!tokenValid) {
            // Clear stored session if we have no valid refresh token to attempt session refresh
            SessionStore.session = null;
            return false;
        }
        try {
            const session = await this.postForm<ISession, IRefreshSessionRequest>(this.sessionUrl, {
                body: {
                    refresh_token: SessionStore.session!.refresh_token,
                    grant_type: GrantType.RefreshToken
                },
                headers: {
                    'Authorization': `${__WEB_BASIC_AUTH}`
                }
            });
            console.debug('Refresh Session response: ', session);
            SessionStore.session = session;
            return true;
        }
        catch (err: unknown) {
            SessionStore.session = null;
            return false;
        }
    }

    async checkAuth() {
        const tokenValid = SessionStore.session && SessionStore.session.access_token && new Date() < new Date(SessionStore.session.expires_at);
        if (tokenValid) {
            return true;

        }
        // If token expired try and refresh
        return await this.refreshSession();
    }

    logOut() {
        SessionStore.session = null;
        // TODO: call api to kill session
    }

    async getAuthCode() {
        const authResponse = await this.get<IAuthCodeResponse>(this.authCode);
        console.debug('Auth code response: ', authResponse);
        return authResponse.authorizationCode;
    }

}
