import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdUploadDataComponent } from './ad-upload-data.component';

describe('AdUploadDataComponent', () => {
  let component: AdUploadDataComponent;
  let fixture: ComponentFixture<AdUploadDataComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ AdUploadDataComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(AdUploadDataComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
