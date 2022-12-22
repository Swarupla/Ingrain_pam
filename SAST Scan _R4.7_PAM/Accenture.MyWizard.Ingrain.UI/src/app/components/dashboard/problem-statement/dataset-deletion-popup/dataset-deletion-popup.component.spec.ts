import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DatasetDeletionPopupComponent } from './dataset-deletion-popup.component';

describe('DatasetDeletionPopupComponent', () => {
  let component: DatasetDeletionPopupComponent;
  let fixture: ComponentFixture<DatasetDeletionPopupComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DatasetDeletionPopupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DatasetDeletionPopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
