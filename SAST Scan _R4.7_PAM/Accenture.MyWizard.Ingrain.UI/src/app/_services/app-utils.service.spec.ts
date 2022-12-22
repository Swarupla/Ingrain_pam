import { TestBed } from '@angular/core/testing';

import { AppUtilsService } from './app-utils.service';

describe('AppUtilsService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: AppUtilsService = TestBed.get(AppUtilsService);
    expect(service).toBeTruthy();
  });
});
