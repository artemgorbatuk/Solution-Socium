import { TestBed } from '@angular/core/testing';
import { signInAs, signOut } from '../account/auth.testing';
import { CurrentUser } from './current-user';

describe('CurrentUser', () => {
  it('Id_Read_WithoutSession_ShouldBeNull', () => {
    expect(TestBed.inject(CurrentUser).id()).toBeNull();
  });

  it('Id_Read_WithSessionStartedAndCleared_ShouldFollowSessionUser', () => {
    const currentUser = TestBed.inject(CurrentUser);

    signInAs('user-1');
    expect(currentUser.id()).toBe('user-1');

    signOut();
    expect(currentUser.id()).toBeNull();
  });
});
