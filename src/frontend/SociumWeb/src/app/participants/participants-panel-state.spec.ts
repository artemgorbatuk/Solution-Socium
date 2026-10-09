import { TestBed } from '@angular/core/testing';
import { ParticipantsPanelState, participantsPanelOpenStorageKey } from './participants-panel-state';

describe('ParticipantsPanelState', () => {
  afterEach(() => localStorage.removeItem(participantsPanelOpenStorageKey));

  it('Open_Read_WithoutSavedChoice_ShouldBeClosed', () => {
    expect(TestBed.inject(ParticipantsPanelState).open()).toBe(false);
  });

  it('Open_Read_WithSavedOpenChoice_ShouldBeOpen', () => {
    localStorage.setItem(participantsPanelOpenStorageKey, 'true');

    expect(TestBed.inject(ParticipantsPanelState).open()).toBe(true);
  });

  it('Toggle_Click_WithClosedPanel_ShouldOpenAndSaveChoice', () => {
    const state = TestBed.inject(ParticipantsPanelState);

    state.toggle();

    expect(state.open()).toBe(true);
    expect(localStorage.getItem(participantsPanelOpenStorageKey)).toBe('true');
  });

  it('Close_Click_WithOpenPanel_ShouldCloseAndSaveChoice', () => {
    localStorage.setItem(participantsPanelOpenStorageKey, 'true');
    const state = TestBed.inject(ParticipantsPanelState);

    state.close();

    expect(state.open()).toBe(false);
    expect(localStorage.getItem(participantsPanelOpenStorageKey)).toBe('false');
  });
});
