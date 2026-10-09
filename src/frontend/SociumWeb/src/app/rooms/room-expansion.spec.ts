import { TestBed } from '@angular/core/testing';
import { RoomExpansion, collapsedRoomsStorageKey } from './room-expansion';

describe('RoomExpansion', () => {
  function create(): RoomExpansion {
    return TestBed.inject(RoomExpansion);
  }

  function saved(): unknown {
    return JSON.parse(localStorage.getItem(collapsedRoomsStorageKey) ?? 'null');
  }

  beforeEach(() => localStorage.clear());

  it('Room_IsExpanded_WithNoSavedState_ShouldBeExpanded', () => {
    expect(create().isExpanded('1')).toBe(true);
  });

  it('Room_Toggle_WithExpandedRoom_ShouldCollapseAndSaveThenExpandAndRemoveKey', () => {
    const expansion = create();

    expansion.toggle('1');
    expect(expansion.isExpanded('1')).toBe(false);
    expect(saved()).toEqual(['1']);

    expansion.toggle('1');
    expect(expansion.isExpanded('1')).toBe(true);
    expect(localStorage.getItem(collapsedRoomsStorageKey)).toBeNull();
  });

  it('Room_Restore_WithSavedCollapsedRoom_ShouldStartCollapsed', () => {
    localStorage.setItem(collapsedRoomsStorageKey, JSON.stringify(['1']));

    const expansion = create();

    expect(expansion.isExpanded('1')).toBe(false);
    expect(expansion.isExpanded('2')).toBe(true);
  });

  it('Room_Restore_WithBrokenSavedState_ShouldExpandAll', () => {
    localStorage.setItem(collapsedRoomsStorageKey, '{oops');

    expect(create().isExpanded('1')).toBe(true);
  });

  it('Room_Expand_WithCollapsedRoom_ShouldExpandAndKeepOthers', () => {
    localStorage.setItem(collapsedRoomsStorageKey, JSON.stringify(['1', '2']));
    const expansion = create();

    expansion.expand('1');

    expect(expansion.isExpanded('1')).toBe(true);
    expect(saved()).toEqual(['2']);
  });

  it('Rooms_Retain_WithDeletedRoom_ShouldForgetIt', () => {
    localStorage.setItem(collapsedRoomsStorageKey, JSON.stringify(['1', '2']));
    const expansion = create();

    expansion.retain(['2', '3']);

    expect(saved()).toEqual(['2']);
    expect(expansion.isExpanded('2')).toBe(false);
  });
});
