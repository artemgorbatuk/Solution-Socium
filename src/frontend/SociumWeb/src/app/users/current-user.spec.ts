import { TestBed } from '@angular/core/testing';
import { CurrentUser, currentUserStorageKey } from './current-user';

describe('CurrentUser', () => {
  beforeEach(() => localStorage.clear());

  afterEach(() => localStorage.clear());

  it('Id_Create_WithNoSavedUser_ShouldBeNull', () => {
    expect(TestBed.inject(CurrentUser).id()).toBeNull();
  });

  it('Id_Create_WithSavedUser_ShouldRestoreId', () => {
    localStorage.setItem(currentUserStorageKey, 'user-1');

    expect(TestBed.inject(CurrentUser).id()).toBe('user-1');
  });

  it('Select_Call_WithUserId_ShouldSetAndSaveThenClearShouldForget', () => {
    const currentUser = TestBed.inject(CurrentUser);

    currentUser.select('user-2');
    expect(currentUser.id()).toBe('user-2');
    expect(localStorage.getItem(currentUserStorageKey)).toBe('user-2');

    currentUser.clear();
    expect(currentUser.id()).toBeNull();
    expect(localStorage.getItem(currentUserStorageKey)).toBeNull();
  });
});
