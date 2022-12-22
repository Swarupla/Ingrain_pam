import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ModelsTrainingStatusPopupComponent } from './models-training-status-popup.component';

describe('ModelsTrainingStatusPopupComponent', () => {
  let component: ModelsTrainingStatusPopupComponent;
  let fixture: ComponentFixture<ModelsTrainingStatusPopupComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ModelsTrainingStatusPopupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ModelsTrainingStatusPopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
