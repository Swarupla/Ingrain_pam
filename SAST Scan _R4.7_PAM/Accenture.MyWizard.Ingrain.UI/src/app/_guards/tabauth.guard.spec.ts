import { TestBed, async, inject } from '@angular/core/testing';

import { TabauthGuard } from './tabauth.guard';

describe('TabauthGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TabauthGuard]
    });
  });

  it('should ...', inject([TabauthGuard], (guard: TabauthGuard) => {
    expect(guard).toBeTruthy();
  }));
});
