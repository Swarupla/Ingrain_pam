import { TestBed } from '@angular/core/testing';

import { ArchiveModelService } from './archive-model.service';

describe('ArchiveModelService', () => {
  let service: ArchiveModelService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ArchiveModelService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
